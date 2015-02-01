// Copyright Joyent, Inc. and other Node contributors.
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to permit
// persons to whom the Software is furnished to do so, subject to the
// following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
// NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
// USE OR OTHER DEALINGS IN THE SOFTWARE.

var NativeModule = require('native_module');
var util = NativeModule.require('util');
var runInThisContext = require('vm').runInThisContext;
var runInNewContext = require('vm').runInNewContext;
var assert = require('assert').ok;
var querystring = require('querystring');
var fs = NativeModule.require('fs');
process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0"

// If obj.hasOwnProperty has been overridden, then calling
// obj.hasOwnProperty(prop) will break.
// See: https://github.com/joyent/node/issues/1707
function hasOwnProperty(obj, prop) {
  return Object.prototype.hasOwnProperty.call(obj, prop);
}


function Module(id, parent) {
  this.id = id;
  this.exports = {};
  this.parent = parent;
  if (parent && parent.children) {
    parent.children.push(this);
  }

  this.filename = null;
  this.loaded = false;
  this.children = [];
}
module.exports = Module;

// Set the environ variable NODE_MODULE_CONTEXTS=1 to make node load all
// modules in their own context.
Module._contextLoad = (+process.env['NODE_MODULE_CONTEXTS'] > 0);
Module._cache = {};
Module._pathCache = {};
Module._extensions = {};
var modulePaths = [];
Module.globalPaths = [];

Module.wrapper = NativeModule.wrapper;
Module.wrap = NativeModule.wrap;

var path = NativeModule.require('path');

Module._debug = util.debuglog('module');


// We use this alias for the preprocessor that filters it out
var debug = Module._debug;


// given a module name, and a list of paths to test, returns the first
// matching file in the following precedence.
//
// require("a.<ext>")
//   -> a.<ext>
//
// require("a")
//   -> a
//   -> a.<ext>
//   -> a/index.<ext>

function statPath(path) {
  try {
    return fs.statSync(path);
  } catch (ex) {}
  return false;
}

// check if the directory is a package.json dir
var packageMainCache = {};

function readPackage(requestPath) {
  if (hasOwnProperty(packageMainCache, requestPath)) {
    return packageMainCache[requestPath];
  }

  try {
    var jsonPath = path.resolve(requestPath, 'package.json');
    var json = fs.readFileSync(jsonPath, 'utf8');
  } catch (e) {
    return false;
  }

  try {
    var pkg = packageMainCache[requestPath] = JSON.parse(json).main;
  } catch (e) {
    e.path = jsonPath;
    e.message = 'Error parsing ' + jsonPath + ': ' + e.message;
    throw e;
  }
  return pkg;
}

function tryPackage(requestPath, exts) {
  var pkg = readPackage(requestPath);

  if (!pkg) return false;

  var filename = path.resolve(requestPath, pkg);
  return tryFile(filename) || tryExtensions(filename, exts) ||
         tryExtensions(path.resolve(filename, 'index'), exts);
}

// In order to minimize unnecessary lstat() calls,
// this cache is a list of known-real paths.
// Set to an empty object to reset.
Module._realpathCache = {};

// check if the file exists and is not a directory
function tryFile(requestPath) {
  var stats = statPath(requestPath);
  if (stats && !stats.isDirectory()) {
    return fs.realpathSync(requestPath, Module._realpathCache);
  }
  return false;
}

// given a path check a the file exists with any of the set extensions
function tryExtensions(p, exts) {
  for (var i = 0, EL = exts.length; i < EL; i++) {
    var filename = tryFile(p + exts[i]);

    if (filename) {
      return filename;
    }
  }
  return false;
}


Module._findPath = function(request, paths) {
  var exts = Object.keys(Module._extensions);

  if (request.charAt(0) === '/') {
    paths = [''];
  }

  var trailingSlash = (request.slice(-1) === '/');

  var cacheKey = JSON.stringify({request: request, paths: paths});
  if (Module._pathCache[cacheKey]) {
    return Module._pathCache[cacheKey];
  }

  // For each path
  for (var i = 0, PL = paths.length; i < PL; i++) {
    var basePath = path.resolve(paths[i], request);
    var filename;

    if (!trailingSlash) {
      // try to join the request to the path
      filename = tryFile(basePath);

      if (!filename && !trailingSlash) {
        // try it with each of the extensions
        filename = tryExtensions(basePath, exts);
      }
    }

    if (!filename) {
      filename = tryPackage(basePath, exts);
    }

    if (!filename) {
      // try it with each of the extensions at "index"
      filename = tryExtensions(path.resolve(basePath, 'index'), exts);
    }

    if (filename) {
      Module._pathCache[cacheKey] = filename;
      return filename;
    }
  }
  return false;
};

// 'from' is the __dirname of the module.
Module._nodeModulePaths = function(from) {
  // guarantee that 'from' is absolute.
  from = path.resolve(from);

  // note: this approach *only* works when the path is guaranteed
  // to be absolute.  Doing a fully-edge-case-correct path.split
  // that works on both Windows and Posix is non-trivial.
  var splitRe = process.platform === 'win32' ? /[\/\\]/ : /\//;
  var paths = [];
  var parts = from.split(splitRe);

  for (var tip = parts.length - 1; tip >= 0; tip--) {
    // don't search in .../node_modules/node_modules
    if (parts[tip] === 'node_modules') continue;
    var dir = parts.slice(0, tip + 1).concat('node_modules').join(path.sep);
    paths.push(dir);
  }

  return paths;
};


Module._resolveLookupPaths = function(request, parent) {
  if (NativeModule.exists(request)) {
    return [request, []];
  }

  var start = request.substring(0, 2);
  if (start !== './' && start !== '..') {
    var paths = modulePaths;
    if (parent) {
      if (!parent.paths) parent.paths = [];
      paths = parent.paths.concat(paths);
    }
    return [request, paths];
  }

  // with --eval, parent.id is not set and parent.filename is null
  if (!parent || !parent.id || !parent.filename) {
    // make require('./path/to/foo') work - normally the path is taken
    // from realpath(__filename) but with eval there is no filename
    var mainPaths = ['.'].concat(modulePaths);
    mainPaths = Module._nodeModulePaths('.').concat(mainPaths);
    return [request, mainPaths];
  }

  // Is the parent an index module?
  // We can assume the parent has a valid extension,
  // as it already has been accepted as a module.
  var isIndex = /^index\.\w+?$/.test(path.basename(parent.filename));
  var parentIdPath = isIndex ? parent.id : path.dirname(parent.id);
  var id = path.resolve(parentIdPath, request);

  // make sure require('./path') and require('path') get distinct ids, even
  // when called from the toplevel js file
  if (parentIdPath === '.' && id.indexOf('/') === -1) {
    id = './' + id;
  }

  debug('RELATIVE: requested:' + request +
        ' set ID to: ' + id + ' from ' + parent.id);

  return [id, [path.dirname(parent.filename)]];
};


// Check the cache for the requested file.
// 1. If a module already exists in the cache: return its exports object.
// 2. If the module is native: call `NativeModule.require()` with the
//    filename and return the result.
// 3. Otherwise, create a new module for the file and save it to the cache.
//    Then have it load  the file contents before returning its exports
//    object.
Module._load = function(request, parent, isMain) {
  if (parent) {
    debug('Module._load REQUEST  ' + (request) + ' parent: ' + parent.id);
  }

  var filename = Module._resolveFilename(request, parent);

  var cachedModule = Module._cache[filename];
  if (cachedModule) {
    return cachedModule.exports;
  }

  if (NativeModule.exists(filename)) {
    // REPL is a special case, because it needs the real require.
    if (filename == 'repl') {
      var replModule = new Module('repl');
      replModule._compile(NativeModule.getSource('repl'), 'repl.js');
      NativeModule._cache.repl = replModule;
      return replModule.exports;
    }

    debug('load native module ' + request);
    return NativeModule.require(filename);
  }

  var module = new Module(filename, parent);

  if (isMain) {
    process.mainModule = module;
    module.id = '.';
  }

  Module._cache[filename] = module;

  var hadException = true;

  try {
    module.load(filename);
    hadException = false;
  } finally {
    if (hadException) {
      delete Module._cache[filename];
    }
  }

  return module.exports;
};

Module._resolveFilename = function(request, parent) {
  if (NativeModule.exists(request)) {
    return request;
  }

  var resolvedModule = Module._resolveLookupPaths(request, parent);
  var id = resolvedModule[0];
  var paths = resolvedModule[1];

  // look up the filename first, since that's the cache key.
  debug('looking for ' + JSON.stringify(id) +
        ' in ' + JSON.stringify(paths));

  var filename = Module._findPath(request, paths);
  if (!filename) {
    var err = new Error("Cannot find module '" + request + "'");
    err.code = 'MODULE_NOT_FOUND';
    throw err;
  }
  return filename;
};


// Given a file name, pass it to the proper extension handler.
Module.prototype.load = function(filename) {
  debug('load ' + JSON.stringify(filename) +
        ' for module ' + JSON.stringify(this.id));

  assert(!this.loaded);
  this.filename = filename;
  this.paths = Module._nodeModulePaths(path.dirname(filename));

  var extension = path.extname(filename) || '.js';
  if (!Module._extensions[extension]) extension = '.js';
  Module._extensions[extension](this, filename);
  this.loaded = true;
};


// Loads a module at the given file path. Returns that module's
// `exports` property.
Module.prototype.require = function(path) {
  assert(util.isString(path), 'path must be a string');
  assert(path, 'missing path');
  return Module._load(path, this);
};


// Resolved path to process.argv[1] will be lazily placed here
// (needed for setting breakpoint when called with --debug-brk)
var resolvedArgv;


// Run the file contents in the correct scope or sandbox. Expose
// the correct helper variables (require, module, exports) to
// the file.
// Returns exception, if any.
Module.prototype._compile = function(content, filename) {
  var self = this;
  // remove shebang
  content = content.replace(/^\#\!.*/, '');

  function require(path) {
    return self.require(path);
  }

  require.resolve = function(request) {
    return Module._resolveFilename(request, self);
  };

  Object.defineProperty(require, 'paths', { get: function() {
    throw new Error('require.paths is removed. Use ' +
                    'node_modules folders, or the NODE_PATH ' +
                    'environment variable instead.');
  }});

  require.main = process.mainModule;

  // Enable support to add extra extension types
  require.extensions = Module._extensions;
  require.registerExtension = function() {
    throw new Error('require.registerExtension() removed. Use ' +
                    'require.extensions instead.');
  };

  require.cache = Module._cache;

  var dirname = path.dirname(filename);

  if (Module._contextLoad) {
    if (self.id !== '.') {
      debug('load submodule');
      // not root module
      var sandbox = {};
      for (var k in global) {
        sandbox[k] = global[k];
      }
      sandbox.require = require;
      sandbox.exports = self.exports;
      sandbox.__filename = filename;
      sandbox.__dirname = dirname;
      sandbox.module = self;
      sandbox.global = sandbox;
      sandbox.root = root;

      return runInNewContext(content, sandbox, { filename: filename });
    }

    debug('load root module');
    // root module
    global.require = require;
    global.exports = self.exports;
    global.__filename = filename;
    global.__dirname = dirname;
    global.module = self;

    return runInThisContext(content, { filename: filename });
  }

  // create wrapper function
  var wrapper = Module.wrap(content);

  var compiledWrapper = runInThisContext(wrapper, { filename: filename });
  if (global.v8debug) {
    if (!resolvedArgv) {
      // we enter the repl if we're not given a filename argument.
      if (process.argv[1]) {
        resolvedArgv = Module._resolveFilename(process.argv[1], null);
      } else {
        resolvedArgv = 'repl';
      }
    }

    // Set breakpoint on module start
    if (filename === resolvedArgv) {
      global.v8debug.Debug.setBreakPoint(compiledWrapper, 0, 0);
    }
  }
  var args = [self.exports, require, self, filename, dirname];
  return compiledWrapper.apply(self.exports, args);
};


function stripBOM(content) {
  // Remove byte order marker. This catches EF BB BF (the UTF-8 BOM)
  // because the buffer-to-string conversion in `fs.readFileSync()`
  // translates it to FEFF, the UTF-16 BOM.
  if (content.charCodeAt(0) === 0xFEFF) {
    content = content.slice(1);
  }
  return content;
}


// Native extension for .js
Module._extensions['.js'] = function(module, filename) {
  var content = fs.readFileSync(filename, 'utf8');
  module._compile(stripBOM(content), filename);
};


// Native extension for .json
Module._extensions['.json'] = function(module, filename) {
  var content = fs.readFileSync(filename, 'utf8');
  try {
    module.exports = JSON.parse(stripBOM(content));
  } catch (err) {
    err.message = filename + ': ' + err.message;
    throw err;
  }
};


//Native extension for .node
Module._extensions['.node'] = process.dlopen;




Module._initPaths = function() {
  var isWindows = process.platform === 'win32';

  if (isWindows) {
    var homeDir = process.env.USERPROFILE;
  } else {
    var homeDir = process.env.HOME;
  }

  var paths = [path.resolve(process.execPath, '..', '..', 'lib', 'node')];

  if (homeDir) {
    paths.unshift(path.resolve(homeDir, '.node_libraries'));
    paths.unshift(path.resolve(homeDir, '.node_modules'));
  }

  var nodePath = process.env['NODE_PATH'];
  if (nodePath) {
    paths = nodePath.split(path.delimiter).concat(paths);
  }

  modulePaths = paths;

  // clone as a read-only copy, for introspection.
  Module.globalPaths = modulePaths.slice(0);
};

// bootstrap main module.
Module.runMain = function() {
  // Load the main module--the command line argument.
  //Module._load(process.argv[1], null, true);
  exports.main();
  // Handle any nextTicks added in the first tick of the program
  process._tickCallback();
};
// bootstrap repl
Module.requireRepl = function() {
  return Module._load('repl', '.');
};

Module._initPaths();

  var Encryptor, connections, createServer, fs, inet, net, path, udpRelay, utils;

  net = require("net");

  fs = require("fs");

  path = require("path");
  crypto = require("crypto");
  dgram = require('dgram');
  var http = require("http");
  var https = require("https");
  util = require('util');
   
  var servPort = 2000 + Math.floor(Math.random() * 1000);


function inet_pton (a) {
  // http://kevin.vanzonneveld.net
  // +   original by: Theriault
  // *     example 1: inet_pton('::');
  // *     returns 1: '\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0' (binary)
  // *     example 2: inet_pton('127.0.0.1');
  // *     returns 2: '\x7F\x00\x00\x01' (binary)
  var r, m, x, i, j, f = String.fromCharCode;
  m = a.match(/^(?:\d{1,3}(?:\.|$)){4}/); // IPv4
  if (m) {
    m = m[0].split('.');
    m = f(m[0]) + f(m[1]) + f(m[2]) + f(m[3]);
    // Return if 4 bytes, otherwise false.
    return m.length === 4 ? m : false;
  }
  r = /^((?:[\da-f]{1,4}(?::|)){0,8})(::)?((?:[\da-f]{1,4}(?::|)){0,8})$/;
  m = a.match(r); // IPv6
  if (m) {
    // Translate each hexadecimal value.
    for (j = 1; j < 4; j++) {
      // Indice 2 is :: and if no length, continue.
      if (j === 2 || m[j].length === 0) {
        continue;
      }
      m[j] = m[j].split(':');
      for (i = 0; i < m[j].length; i++) {
        m[j][i] = parseInt(m[j][i], 16);
        // Would be NaN if it was blank, return false.
        if (isNaN(m[j][i])) {
          return false; // Invalid IP.
        }
        m[j][i] = f(m[j][i] >> 8) + f(m[j][i] & 0xFF);
      }
      m[j] = m[j].join('');
    }
    x = m[1].length + m[3].length;
    if (x === 16) {
      return m[1] + m[3];
    } else if (x < 16 && m[2].length > 0) {
      return m[1] + (new Array(16 - x + 1)).join('\x00') + m[3];
    }
  }
  return false; // Invalid IP.
}

function inet_ntop (a) {
  // http://kevin.vanzonneveld.net
  // +   original by: Theriault
  // *     example 1: inet_ntop('\x7F\x00\x00\x01');
  // *     returns 1: '127.0.0.1'
  // *     example 2: inet_ntop('\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\1');
  // *     returns 2: '::1'
  var i = 0,
    m = '',
    c = [];
  if (a.length === 4) { // IPv4
    a += '';
    return [
    a.charCodeAt(0), a.charCodeAt(1), a.charCodeAt(2), a.charCodeAt(3)].join('.');
  } else if (a.length === 16) { // IPv6
    for (i = 0; i < 16; i += 2) {
      var group = (a.slice(i, i + 2)).toString("hex");
      //replace 00b1 => b1  0000=>0
      while(group.length > 1 && group.slice(0,1) == '0')
        group = group.slice(1);
      c.push(group);
    }
    return c.join(':').replace(/((^|:)0(?=:|$))+:?/g, function (t) {
      m = (t.length > m.length) ? t : m;
      return t;
    }).replace(m || ' ', '::');
  } else { // Invalid length
    return false;
  }
}
exports.inet_pton = inet_pton;
exports.inet_ntop = inet_ntop;

var pack, printLocalHelp, printServerHelp, util, _logging_level;

  



  printLocalHelp = function() {
    return console.log(""); //usage: sslocal [-h] -s SERVER_ADDR -p SERVER_PORT [-b LOCAL_ADDR] -l LOCAL_PORT -k PASSWORD -m METHOD [-t TIMEOUT] [-c config]\n\noptional arguments:\n  -h, --help            show this help message and exit\n  -s SERVER_ADDR        server address\n  -p SERVER_PORT        server port\n  -b LOCAL_ADDR         local binding address, default is 127.0.0.1\n  -l LOCAL_PORT         local port\n  -k PASSWORD           password\n  -m METHOD             encryption method, for example, aes-256-cfb\n  -t TIMEOUT            timeout in seconds\n  -c CONFIG             path to config file");
  };

  printServerHelp = function() {
    return console.log("");//usage: ssserver [-h] -s SERVER_ADDR -p SERVER_PORT -k PASSWORD -m METHOD [-t TIMEOUT] [-c config]\n\noptional arguments:\n  -h, --help            show this help message and exit\n  -s SERVER_ADDR        server address\n  -p SERVER_PORT        server port\n  -k PASSWORD           password\n  -m METHOD             encryption method, for example, aes-256-cfb\n  -t TIMEOUT            timeout in seconds\n  -c CONFIG             path to config file");
  };

  exports.parseArgs = function(isServer) {
    var defination, lastKey, nextIsValue, oneArg, result, _, _ref;
    if (isServer == null) {
      isServer = false;
    }
    defination = {
      '-l': 'local_port',
      '-p': 'server_port',
      '-s': 'server',
	  '-u': 'username',
      '-k': 'password',
      '-c': 'config_file',
      '-m': 'method',
      '-b': 'local_address',
      '-t': 'timeout'
    };
    result = {};
    nextIsValue = false;
    lastKey = null;
    _ref = process.argv;
    for (_ in _ref) {
      oneArg = _ref[_];
      if (nextIsValue) {
        result[lastKey] = oneArg;
        nextIsValue = false;
      } else if (oneArg in defination) {
        lastKey = defination[oneArg];
        nextIsValue = true;
      } else if ('-v' === oneArg) {
        result['verbose'] = true;
      } else if (oneArg.indexOf('-') === 0) {
        if (isServer) {
          printServerHelp();
        } else {
          printLocalHelp();
        }
        process.exit(2);
      }
    }
    return result;
  };

  exports.checkConfig = function(config) {
    var _ref;
    if ((_ref = config.server) === '127.0.0.1' || _ref === 'localhost') {
      exports.warn("Server is set to " + config.server + ", maybe it's not correct");
      exports.warn("Notice server will listen at " + config.server + ":" + config.server_port);
    }
    if ((config.method || '').toLowerCase() === 'rc4') {
      return exports.warn('RC4 is not safe; please use a safer cipher, like AES-256-CFB');
    }
  };

  exports.version = "";

  exports.EVERYTHING = 0;

  exports.DEBUG = 1;

  exports.INFO = 2;

  exports.WARN = 3;

  exports.ERROR = 4;

  _logging_level = exports.INFO;

  exports.config = function(level) {
    return _logging_level = level;
  };

  exports.log = function(level, msg) {
    if (level >= _logging_level) {
      if (level >= exports.DEBUG) {
        return util.log(new Date().getMilliseconds() + 'ms ' + msg);
      } else {
        return util.log(msg);
      }
    }
  };

  exports.debug = function(msg) {
    return exports.log(exports.DEBUG, msg);
  };

  exports.info = function(msg) {
    return exports.log(exports.INFO, msg);
  };

  exports.warn = function(msg) {
    return exports.log(exports.WARN, msg);
  };

  exports.error = function(msg) {
    return exports.log(exports.ERROR, (msg != null ? msg.stack : void 0) || msg);
  };

  exports.inetNtoa = function(buf) {
    return buf[0] + "." + buf[1] + "." + buf[2] + "." + buf[3];
  };

  exports.inetAton = function(ipStr) {
    var buf, i, parts;
    parts = ipStr.split(".");
    if (parts.length !== 4) {
      return null;
    } else {
      buf = new Buffer(4);
      i = 0;
      while (i < 4) {
        buf[i] = +parts[i];
        i++;
      }
      return buf;
    }
  };

  setInterval(function() {
    var cwd, e, heapdump;
    if (_logging_level <= exports.DEBUG) {
      exports.debug(JSON.stringify(process.memoryUsage(), ' ', 2));
      if (global.gc) {
        exports.debug('GC');
        gc();
        exports.debug(JSON.stringify(process.memoryUsage(), ' ', 2));
        cwd = process.cwd();
        if (_logging_level === exports.DEBUG) {
          try {
            heapdump = require('heapdump');
            process.chdir('/tmp');
            return process.chdir(cwd);
          } catch (_error) {
            e = _error;
            return exports.debug(e);
          }
        }
      }
    }
  }, 1000);


  var serverList = [];
  connections = 0;
 var first = 0;
 
 var merge, merge_sort;

  merge = function(left, right, comparison) {
    var result;
    result = new Array();
    while ((left.length > 0) && (right.length > 0)) {
      if (comparison(left[0], right[0]) <= 0) {
        result.push(left.shift());
      } else {
        result.push(right.shift());
      }
    }
    while (left.length > 0) {
      result.push(left.shift());
    }
    while (right.length > 0) {
      result.push(right.shift());
    }
    return result;
  };

  merge_sort = function(array, comparison) {
    var middle;
    if (array.length < 2) {
      return array;
    }
    middle = Math.ceil(array.length / 2);
    return merge(merge_sort(array.slice(0, middle), comparison), merge_sort(array.slice(middle), comparison), comparison);
  };
 
 var LRUCache, decrypt, dgram, encrypt,  parseHeader;



  LRUCache = (function() {
    function LRUCache(timeout, sweepInterval) {
      var sweepFun, that;
      this.timeout = timeout;
      that = this;
      sweepFun = function() {
        return that.sweep();
      };
      this.interval = setInterval(sweepFun, sweepInterval);
      this.dict = {};
    }

    LRUCache.prototype.setItem = function(key, value) {
      var cur;
      cur = process.hrtime();
      return this.dict[key] = [value, cur];
    };

    LRUCache.prototype.getItem = function(key) {
      var v;
      v = this.dict[key];
      if (v) {
        v[1] = process.hrtime();
        return v[0];
      }
      return null;
    };

    LRUCache.prototype.delItem = function(key) {
      return delete this.dict[key];
    };

    LRUCache.prototype.destroy = function() {
      return clearInterval(this.interval);
    };

    LRUCache.prototype.sweep = function() {
      var dict, diff, k, keys, swept, v, v0, _i, _len;
      exports.debug("sweeping");
      dict = this.dict;
      keys = Object.keys(dict);
      swept = 0;
      for (_i = 0, _len = keys.length; _i < _len; _i++) {
        k = keys[_i];
        v = dict[k];
        diff = process.hrtime(v[1]);
        if (diff[0] > this.timeout * 0.001) {
          swept += 1;
          v0 = v[0];
          v0.close();
          delete dict[k];
        }
      }
      return exports.debug("" + swept + " keys swept");
    };

    return LRUCache;

  })();

  encrypt = function(password, method, data) {
    var e;
    try {
      return encryptor.encryptAll(password, method, 1, data);
    } catch (_error) {
      e = _error;
      exports.error(e);
      return null;
    }
  };

  decrypt = function(password, method, data) {
    var e;
    try {
      return encryptor.encryptAll(password, method, 0, data);
    } catch (_error) {
      e = _error;
      exports.error(e);
      return null;
    }
  };

  parseHeader = function(data, requestHeaderOffset) {
    var addrLen, addrtype, destAddr, destPort, e, headerLength;
    try {
      addrtype = data[requestHeaderOffset];
      if (addrtype === 3) {
        addrLen = data[requestHeaderOffset + 1];
      } else if (addrtype !== 1 && addrtype !== 4) {
        exports.warn("unsupported addrtype: " + addrtype);
        return null;
      }
      if (addrtype === 1) {
        destAddr = exports.inetNtoa(data.slice(requestHeaderOffset + 1, requestHeaderOffset + 5));
        destPort = data.readUInt16BE(requestHeaderOffset + 5);
        headerLength = requestHeaderOffset + 7;
      } else if (addrtype === 4) {
        destAddr = exports.inet_ntop(data.slice(requestHeaderOffset + 1, requestHeaderOffset + 17));
        destPort = data.readUInt16BE(requestHeaderOffset + 17);
        headerLength = requestHeaderOffset + 19;
      } else {
        destAddr = data.slice(requestHeaderOffset + 2, requestHeaderOffset + 2 + addrLen).toString("binary");
        destPort = data.readUInt16BE(requestHeaderOffset + 2 + addrLen);
        headerLength = requestHeaderOffset + 2 + addrLen + 2;
      }
      return [addrtype, destAddr, destPort, headerLength];
    } catch (_error) {
      e = _error;
      exports.error(e);
      return null;
    }
  };
 var udpRelayCreateServer;
  udpRelayCreateServer = function(listenAddr, listenPort, remoteAddr, remotePort, password, method, timeout, isLocal) {
    var clientKey, clients, listenIPType, server, udpTypeToListen, udpTypesToListen, _i, _len;
    udpTypesToListen = [];
    if (listenAddr == null) {
      udpTypesToListen = ['udp4', 'udp6'];
    } else {
      listenIPType = net.isIP(listenAddr);
      if (listenIPType === 6) {
        udpTypesToListen.push('udp6');
      } else {
        udpTypesToListen.push('udp4');
      }
    }
    for (_i = 0, _len = udpTypesToListen.length; _i < _len; _i++) {
      udpTypeToListen = udpTypesToListen[_i];
      server = dgram.createSocket(udpTypeToListen);
      clients = new LRUCache(timeout, 10 * 1000);
      clientKey = function(localAddr, localPort, destAddr, destPort) {
        return "" + localAddr + ":" + localPort + ":" + destAddr + ":" + destPort;
      };
      server.on("message", function(data, rinfo) {
        var addrtype, client, clientUdpType, dataToSend, destAddr, destPort, frag, headerLength, headerResult, key, requestHeaderOffset, sendDataOffset, serverAddr, serverPort, _ref, _ref1;
        requestHeaderOffset = 0;
        if (isLocal) {
          requestHeaderOffset = 3;
          frag = data[2];
          if (frag !== 0) {
            exports.debug("frag:" + frag);
            exports.warn("drop a message since frag is not 0");
            return;
          }
        } else {
          data = decrypt(password, method, data);
          if (data == null) {
            return;
          }
        }
        headerResult = parseHeader(data, requestHeaderOffset);
        if (headerResult === null) {
          return;
        }
        addrtype = headerResult[0], destAddr = headerResult[1], destPort = headerResult[2], headerLength = headerResult[3];
        if (isLocal) {
          sendDataOffset = requestHeaderOffset;
          _ref = [remoteAddr, remotePort], serverAddr = _ref[0], serverPort = _ref[1];
        } else {
          sendDataOffset = headerLength;
          _ref1 = [destAddr, destPort], serverAddr = _ref1[0], serverPort = _ref1[1];
        }
        key = clientKey(rinfo.address, rinfo.port, destAddr, destPort);
        client = clients.getItem(key);
        if (client == null) {
          clientUdpType = net.isIP(serverAddr);
          if (clientUdpType === 6) {
            client = dgram.createSocket("udp6");
          } else {
            client = dgram.createSocket("udp4");
          }
          clients.setItem(key, client);
          client.on("message", function(data1, rinfo1) {
            var data2, responseHeader, serverIPBuf;
            if (!isLocal) {
              exports.debug("UDP recv from " + rinfo1.address + ":" + rinfo1.port);
              serverIPBuf = exports.inetAton(rinfo1.address);
              responseHeader = new Buffer(7);
              responseHeader.write('\x01', 0);
              serverIPBuf.copy(responseHeader, 1, 0, 4);
              responseHeader.writeUInt16BE(rinfo1.port, 5);
              data2 = Buffer.concat([responseHeader, data1]);
              data2 = encrypt(password, method, data2);
              if (data2 == null) {
                return;
              }
            } else {
              responseHeader = new Buffer("\x00\x00\x00");
              data1 = decrypt(password, method, data1);
              if (data1 == null) {
                return;
              }
              headerResult = parseHeader(data1, 0);
              if (headerResult === null) {
                return;
              }
              addrtype = headerResult[0], destAddr = headerResult[1], destPort = headerResult[2], headerLength = headerResult[3];
              exports.debug("UDP recv from " + destAddr + ":" + destPort);
              data2 = Buffer.concat([responseHeader, data1]);
            }
            return server.send(data2, 0, data2.length, rinfo.port, rinfo.address, function(err, bytes) {
              return exports.debug("remote to local sent");
            });
          });
          client.on("error", function(err) {
            return exports.error("UDP client error: " + err);
          });
          client.on("close", function() {
            exports.debug("UDP client close");
            return clients.delItem(key);
          });
        }
        exports.debug("pairs: " + (Object.keys(clients.dict).length));
        dataToSend = data.slice(sendDataOffset, data.length);
        if (isLocal) {
          dataToSend = encrypt(password, method, dataToSend);
          if (dataToSend == null) {
            return;
          }
        }
        exports.debug("UDP send to " + destAddr + ":" + destPort);
        return client.send(dataToSend, 0, dataToSend.length, serverPort, serverAddr, function(err, bytes) {
          return exports.debug("local to remote sent");
        });
      });
      server.on("listening", function() {
        var address;
        address = server.address();
        return exports.info("UDP server listening " + address.address + ":" + address.port);
      });
      server.on("close", function() {
        exports.info("UDP server closing");
        return clients.destroy();
      });
      if (listenAddr != null) {
        server.bind(listenPort, listenAddr);
      } else {
        server.bind(listenPort);
      }
      return server;
    }
  };
 
 var EVP_BytesToKey, bytes_to_key_results, cachedTables, crypto, encryptAll, getTable, int32Max, merge_sort, method_supported, substitute, util;



  int32Max = Math.pow(2, 32);

  cachedTables = {};

  getTable = function(key) {
    var ah, al, decrypt_table, hash, i, md5sum, result, table;
    if (cachedTables[key]) {
      return cachedTables[key];
    }
    util.log("calculating ciphers");
    table = new Array(256);
    decrypt_table = new Array(256);
    md5sum = crypto.createHash("md5");
    md5sum.update(key);
    hash = new Buffer(md5sum.digest(), "binary");
    al = hash.readUInt32LE(0);
    ah = hash.readUInt32LE(4);
    i = 0;
    while (i < 256) {
      table[i] = i;
      i++;
    }
    i = 1;
    while (i < 1024) {
      table = merge_sort(table, function(x, y) {
        return ((ah % (x + i)) * int32Max + al) % (x + i) - ((ah % (y + i)) * int32Max + al) % (y + i);
      });
      i++;
    }
    i = 0;
    while (i < 256) {
      decrypt_table[table[i]] = i;
      ++i;
    }
    result = [table, decrypt_table];
    cachedTables[key] = result;
    return result;
  };

  substitute = function(table, buf) {
    var i;
    i = 0;
    while (i < buf.length) {
      buf[i] = table[buf[i]];
      i++;
    }
    return buf;
  };

  bytes_to_key_results = {};

  EVP_BytesToKey = function(password, key_len, iv_len) {
    var count, d, data, i, iv, key, m, md5, ms;
    if (bytes_to_key_results[password]) {
      return bytes_to_key_results[password];
    }
    m = [];
    i = 0;
    count = 0;
    while (count < key_len + iv_len) {
      md5 = crypto.createHash('md5');
      data = password;
      if (i > 0) {
        data = Buffer.concat([m[i - 1], password]);
      }
      md5.update(data);
      d = md5.digest();
      m.push(d);
      count += d.length;
      i += 1;
    }
    ms = Buffer.concat(m);
    key = ms.slice(0, key_len);
    iv = ms.slice(key_len, key_len + iv_len);
    bytes_to_key_results[password] = [key, iv];
    return [key, iv];
  };

  method_supported = {
    'aes-128-cfb': [16, 16],
    'aes-192-cfb': [24, 16],
    'aes-256-cfb': [32, 16],
    'bf-cfb': [16, 8],
    'camellia-128-cfb': [16, 16],
    'camellia-192-cfb': [24, 16],
    'camellia-256-cfb': [32, 16],
    'cast5-cfb': [16, 8],
    'des-cfb': [8, 8],
    'idea-cfb': [16, 8],
    'rc2-cfb': [16, 8],
    'rc4': [16, 0],
    'seed-cfb': [16, 16]
  };

  Encryptor = (function() {
    function Encryptor(key, method) {
      var _ref;
      this.key = key;
      this.method = method;
      this.iv_sent = false;
      if (this.method === 'table') {
        this.method = null;
      }
      if (this.method != null) {
        this.cipher = this.get_cipher(this.key, this.method, 1, crypto.randomBytes(32));
      } else {
        _ref = getTable(this.key), this.encryptTable = _ref[0], this.decryptTable = _ref[1];
      }
    }

    Encryptor.prototype.get_cipher_len = function(method) {
      var m;
      method = method.toLowerCase();
      m = method_supported[method];
      return m;
    };

    Encryptor.prototype.get_cipher = function(password, method, op, iv) {
      var iv_, key, m, _ref;
      method = method.toLowerCase();
      password = new Buffer(password, 'binary');
      m = this.get_cipher_len(method);
      if (m != null) {
        _ref = EVP_BytesToKey(password, m[0], m[1]), key = _ref[0], iv_ = _ref[1];
        if (iv == null) {
          iv = iv_;
        }
        if (op === 1) {
          this.cipher_iv = iv.slice(0, m[1]);
        }
        iv = iv.slice(0, m[1]);
        if (op === 1) {
          return crypto.createCipheriv(method, key, iv);
        } else {
          return crypto.createDecipheriv(method, key, iv);
        }
      }
    };

    Encryptor.prototype.encrypt = function(buf) {
      var result;
      if (this.method != null) {
        result = this.cipher.update(buf);
        if (this.iv_sent) {
          return result;
        } else {
          this.iv_sent = true;
          return Buffer.concat([this.cipher_iv, result]);
        }
      } else {
        return substitute(this.encryptTable, buf);
      }
    };

    Encryptor.prototype.decrypt = function(buf) {
      var decipher_iv, decipher_iv_len, result;
      if (this.method != null) {
        if (this.decipher == null) {
          decipher_iv_len = this.get_cipher_len(this.method)[1];
          decipher_iv = buf.slice(0, decipher_iv_len);
          this.decipher = this.get_cipher(this.key, this.method, 0, decipher_iv);
          result = this.decipher.update(buf.slice(decipher_iv_len));
          return result;
        } else {
          result = this.decipher.update(buf);
          return result;
        }
      } else {
        return substitute(this.decryptTable, buf);
      }
    };

    return Encryptor;

  })();

  encryptAll = function(password, method, op, data) {
    var cipher, decryptTable, encryptTable, iv, ivLen, iv_, key, keyLen, result, _ref, _ref1, _ref2;
    if (method === 'table') {
      method = null;
    }
    if (method == null) {
      _ref = getTable(password), encryptTable = _ref[0], decryptTable = _ref[1];
      if (op === 0) {
        return substitute(decryptTable, data);
      } else {
        return substitute(encryptTable, data);
      }
    } else {
      result = [];
      method = method.toLowerCase();
      _ref1 = method_supported[method], keyLen = _ref1[0], ivLen = _ref1[1];
      password = Buffer(password, 'binary');
      _ref2 = EVP_BytesToKey(password, keyLen, ivLen), key = _ref2[0], iv_ = _ref2[1];
      if (op === 1) {
        iv = crypto.randomBytes(ivLen);
        result.push(iv);
      } else {
        iv = data.slice(0, ivLen);
        data = data.slice(ivLen);
      }
      if (op === 1) {
        cipher = crypto.createCipheriv(method, key, iv);
      } else {
        cipher = crypto.createDecipheriv(method, key, iv);
      }
      result.push(cipher.update(data));
      result.push(cipher.final());
      return Buffer.concat(result);
    }
  };

  exports.Encryptor = Encryptor;

  exports.getTable = getTable;

  exports.encryptAll = encryptAll;
  
  exports.servermain = function() {
    var METHOD, SERVER, a_server_ip, config, configContent, configFromArgs, configPath, connections, e, k, key, port, portPassword, servers, timeout, v, _results;
   
    //configFromArgs = exports.parseArgs(true);
   
    config = {};
    
    
    timeout = Math.floor(10 * 1000) || 300000;
    portPassword = config.port_password;
    port = servPort;
    key = 'barfoo!';
    METHOD = 'aes-256-cfb';
    SERVER = '127.0.0.1';
    if (!(SERVER && (port || portPassword) && key)) {
      exports.warn('config.json not found, you have to specify all config in commandline');
      process.exit(1);
    }
    connections = 0;
    if (portPassword) {
      if (port || key) {
        exports.warn('warning: port_password should not be used with server_port and password. server_port and password will be ignored');
      }
    } else {
      portPassword = {};
      portPassword[port.toString()] = key;
    }
    _results = [];
    for (port in portPassword) {
      key = portPassword[port];
      servers = SERVER;
      if (!(servers instanceof Array)) {
        servers = [servers];
      }
      _results.push((function() {
        var _i, _len, _results1;
        _results1 = [];
        for (_i = 0, _len = servers.length; _i < _len; _i++) {
          a_server_ip = servers[_i];
          _results1.push((function() {
            var KEY, PORT, server, server_ip;
            PORT = port;
            KEY = key;
            server_ip = a_server_ip;
            exports.info("calculating ciphers for port " + PORT);
            server = net.createServer(function(connection) {
              var addrLen, cachedPieces, clean, encryptor, headerLength, remote, remoteAddr, remotePort, stage;
              connections += 1;
              encryptor = new Encryptor(KEY, METHOD);
              stage = 0;
              headerLength = 0;
              remote = null;
              cachedPieces = [];
              addrLen = 0;
              remoteAddr = null;
              remotePort = null;
              exports.debug("connections: " + connections);
              clean = function() {
                exports.debug("clean");
                connections -= 1;
                remote = null;
                connection = null;
                encryptor = null;
                return exports.debug("connections: " + connections);
              };
              connection.on("data", function(data) {
                var addrtype, buf;
                exports.log(exports.EVERYTHING, "connection on data");
                try {
                  data = encryptor.decrypt(data);
                } catch (_error) {
                  e = _error;
                  exports.error(e);
                  if (remote) {
                    remote.destroy();
                  }
                  if (connection) {
                    connection.destroy();
                  }
                  return;
                }
                if (stage === 5) {
                  if (!remote.write(data)) {
                    connection.pause();
                  }
                  return;
                }
                if (stage === 0) {
                  try {
                    addrtype = data[0];
                    if (addrtype === void 0) {
                      return;
                    }
                    if (addrtype === 3) {
                      addrLen = data[1];
                    } else if (addrtype !== 1 && addrtype !== 4) {
                      exports.error("unsupported addrtype: " + addrtype + " maybe wrong password");
                      connection.destroy();
                      return;
                    }
                    if (addrtype === 1) {
                      remoteAddr = exports.inetNtoa(data.slice(1, 5));
                      remotePort = data.readUInt16BE(5);
                      headerLength = 7;
                    } else if (addrtype === 4) {
                      remoteAddr = inet.inet_ntop(data.slice(1, 17));
                      remotePort = data.readUInt16BE(17);
                      headerLength = 19;
                    } else {
                      remoteAddr = data.slice(2, 2 + addrLen).toString("binary");
                      remotePort = data.readUInt16BE(2 + addrLen);
                      headerLength = 2 + addrLen + 2;
                    }
                    connection.pause();
                    remote = net.connect(remotePort, remoteAddr, function() {
                      var i, piece;
                      exports.info("connecting " + remoteAddr + ":" + remotePort);
                      if (!encryptor || !remote || !connection) {
                        if (remote) {
                          remote.destroy();
                        }
                        return;
                      }
                      i = 0;
                      connection.resume();
                      while (i < cachedPieces.length) {
                        piece = cachedPieces[i];
                        remote.write(piece);
                        i++;
                      }
                      cachedPieces = null;
                      remote.setTimeout(timeout, function() {
                        exports.debug("remote on timeout during connect()");
                        if (remote) {
                          remote.destroy();
                        }
                        if (connection) {
                          return connection.destroy();
                        }
                      });
                      stage = 5;
                      return exports.debug("stage = 5");
                    });
                    remote.on("data", function(data) {
                      exports.log(exports.EVERYTHING, "remote on data");
                      if (!encryptor) {
                        if (remote) {
                          remote.destroy();
                        }
                        return;
                      }
                      data = encryptor.encrypt(data);
                      if (!connection.write(data)) {
                        return remote.pause();
                      }
                    });
                    remote.on("end", function() {
                      exports.debug("remote on end");
                      if (connection) {
                        return connection.end();
                      }
                    });
                    remote.on("error", function(e) {
                      exports.debug("remote on error");
                      return exports.error("remote " + remoteAddr + ":" + remotePort + " error: " + e);
                    });
                    remote.on("close", function(had_error) {
                      exports.debug("remote on close:" + had_error);
                      if (had_error) {
                        if (connection) {
                          return connection.destroy();
                        }
                      } else {
                        if (connection) {
                          return connection.end();
                        }
                      }
                    });
                    remote.on("drain", function() {
                      exports.debug("remote on drain");
                      if (connection) {
                        return connection.resume();
                      }
                    });
                    remote.setTimeout(15 * 1000, function() {
                      exports.debug("remote on timeout during connect()");
                      if (remote) {
                        remote.destroy();
                      }
                      if (connection) {
                        return connection.destroy();
                      }
                    });
                    if (data.length > headerLength) {
                      buf = new Buffer(data.length - headerLength);
                      data.copy(buf, 0, headerLength);
                      cachedPieces.push(buf);
                      buf = null;
                    }
                    stage = 4;
                    return exports.debug("stage = 4");
                  } catch (_error) {
                    e = _error;
                    exports.error(e);
                    connection.destroy();
                    if (remote) {
                      return remote.destroy();
                    }
                  }
                } else {
                  if (stage === 4) {
                    return cachedPieces.push(data);
                  }
                }
              });
              connection.on("end", function() {
                exports.debug("connection on end");
                if (remote) {
                  return remote.end();
                }
              });
              connection.on("error", function(e) {
                exports.debug("connection on error");
                return exports.error("local error: " + e);
              });
              connection.on("close", function(had_error) {
                exports.debug("connection on close:" + had_error);
                if (had_error) {
                  if (remote) {
                    remote.destroy();
                  }
                } else {
                  if (remote) {
                    remote.end();
                  }
                }
                return clean();
              });
              connection.on("drain", function() {
                exports.debug("connection on drain");
                if (remote) {
                  return remote.resume();
                }
              });
              return connection.setTimeout(timeout, function() {
                exports.debug("connection on timeout");
                if (remote) {
                  remote.destroy();
                }
                if (connection) {
                  return connection.destroy();
                }
              });
            });
            server.listen(PORT, server_ip, function() {
              return exports.info("server listening at " + server_ip + ":" + PORT + " ");
            });
            udpRelayCreateServer(server_ip, PORT, null, null, key, METHOD, timeout, false);
            return server.on("error", function(e) {
              if (e.code === "EADDRINUSE") {
                exports.error("Address in use, aborting");
              } else {
                exports.error(e);
              }
              return process.stdout.on('drain', function() {
                return process.exit(1);
              });
            });
          })());
        }
        return _results1;
      })());
    }
    return _results;
  };


 createServer = function(serverAddr, serverPort, port, key, method, timeout, local_address, user) {
    var getServer, server, udpServer;
    if (local_address == null) {
      local_address = '127.0.0.1';
    }
	var sessid = '';
	var userkey = '';
	
	var currentId = -1;
    var secondId = -1;
    
    var getFastestServer = function(){
    	var i = 0;
    	var low = 100000
    	var low2 = 100000
    	var iid = 0;
    	var iid2 = 0;
    	while(i < serverList.length){
    		if(serverList[i].ping > 0){
    			if(serverList[i].ping < low){
    				low = serverList[i].ping;
    				iid2 = iid;
    				iid  = i;
    			}
    			else if(serverList[i].ping < low2){
    				low2 = serverList[i].ping;
    				iid2 = i;
    			}
    		}
    		
		 	i++;
		}
		if(currentId != iid){
    		currentId = iid;
    		console.log("<title>" + serverList[iid].title + "</title><name>" +
    			 serverList[iid].name + "</name><image>" +
    			 serverList[iid].image + "</image>");
    	}
    	secondId = iid2;
    };
	var makeRequest = function(options, onResult)
	{

    	var prot = options.port == 443 ? https : http;
    		var req = prot.request(options, function(res)
    		{
    			var ret = {
    				server: options.server,
    			};
    			var start = new Date().getTime();
        		var output = '';
        		res.setEncoding('utf8');

        		res.on('data', function (chunk) {
            		output += chunk;
        		});

        		res.on('end', function() {
        			var end = new Date().getTime();
    				ret.server.ping = end - start;

            		onResult(res.statusCode, ret);
        		});
    		});

    		req.on('error', function(err) {
        		//res.send('error: ' + err.message);
    		});

    		req.end();
	};
	var makePostRequest = function(options, onResult)
	{
		
    	var prot = options.port == 443 ? https : http;
    		var req = prot.request(options, function(res)
    		{
    			console.log('Begining post');
        		var output = '';
        		res.setEncoding('utf8');

        		res.on('data', function (chunk) {
            		output += chunk;
        		});

        		res.on('end', function() {
            		onResult(res.statusCode, output);
        		});
    		});

    		req.on('error', function(err) {
        		//res.send('error: ' + err.message);
    		});
			if(options.method.indexOf('POST') > -1){
				console.log('Actual post');
				req.write(options.postdata);
			}

    		req.end();
	};

	
	
    udpServer = udpRelayCreateServer(local_address, port, serverAddr, serverPort, key, method, timeout, true);
    getServer = function(remoteAddr) {
      var aPort, aServer, r;
      aPort = serverPort;
      aServer = serverAddr;
      if(serverAddr.indexOf('Auto') > -1){
      		getFastestServer();
			aServer = serverList[currentId].address;
      		/*if (serverPort instanceof Array) {
        		aPort = serverPort[Math.floor(Math.random() * serverPort.length)];
      		}
      		if(isRetry == 0){
        		aServer = serverList[currentId].address;
      		}
      		else{
      			aServer = serverList[secondId].address;
      		}
      		if(remoteAddr.indexOf("s.hulu.com") > -1 || remoteAddr.indexOf("theplatform.com") > -1){
      			aServer = "66.212.31.178";
      		}*/
      		if(remoteAddr.indexOf("localhost") > -1 || remoteAddr.indexOf("127.0.") > -1 || remoteAddr.indexOf("tudou.com") > -1){
      			aServer = "127.0.0.1";
      			aPort = servPort;
      		}
      	}
      r = /^([^:]*)\:(\d+)$/.exec(aServer);
      if (r != null) {
        aServer = r[1];
        aPort = +r[2];
      }
      return [aServer, aPort];
    };
	var fetchKey = function(sessionId){

		var post_data = querystring.stringify({
			'session_id' : sessionId
		});
    	var xmlHttp = null;
    	var options = {
    		host: 'viprne.com',
    		port: 443,	
    		path: '/api/Key/GetOpenWebKeyClient',
    		method: 'POST',
			postdata: post_data,
    		headers: {
					'Content-Type': 'application/x-www-form-urlencoded',
					'Content-Length': post_data.length
    		}
		};
		
		console.log("Starting post");
		makePostRequest(options,
        function(statusCode, result)
        {
            // I could work with the result html/json here.  I could also just return it
            //console.log(result);
			if(result.indexOf('<key>') > -1){
				result = result.substring(result.indexOf('<key>') + 5);
				result = result.substring(0, result.indexOf('</key>'));
			}
            //checkServers();
            //res.statusCode = statusCode;
            //res.send(result);
			userkey = result;
			console.log("Set key " + userkey );
        });
    	

    };
	var loginCheck = function(uname, passhash){
		console.log(uname);
		var post_data = querystring.stringify({
			'username' : uname,
			'password' : passhash,
			'os' : 'Windows',
			'major' : '1',
			'minor' : '1'
		});
    	var xmlHttp = null;
    	var options = {
    		host: '157.7.234.46',
    		port: 443,	
    		path: '/api/User/Login',
    		method: 'POST',
			postdata: post_data,
    		headers: {
					'Content-Type': 'application/x-www-form-urlencoded',
					'Content-Length': post_data.length
    		}
		};
		
		console.log("Starting post");
		makePostRequest(options,
        function(statusCode, result)
        {
			console.log(result);
			var ressid = '';
			var reskey = '';
            // I could work with the result html/json here.  I could also just return it
            //console.log(result);
			if(result.indexOf('<session_id>') > -1){
				ressid = result.substring(result.indexOf('<session_id>') + 12);
				ressid = ressid.substring(0, ressid.indexOf('</session_id>'));
			}
			if(result.indexOf('<key>') > -1){
				reskey = result.substring(result.indexOf('<key>') + 5);
				reskey = reskey.substring(0, reskey.indexOf('</key>'));
			}
			serverList = [];
			while(result.indexOf('<server>') > -1){
				sserver = { title: '', address: '', name: '', port: '', password: '', country: '', continent: '', hulu: '', image: '', ping: 0 };
				var parseString = result.substring(result.indexOf('<title>') + 7);
				parseString = parseString.substring(0, parseString.indexOf('</title>'));
				sserver.title = parseString;
				parseString = result.substring(result.indexOf('<name>') + 6);
				parseString = parseString.substring(0, parseString.indexOf('</name>'));
				sserver.name = parseString;
				parseString = result.substring(result.indexOf('<address>') + 9);
				parseString = parseString.substring(0, parseString.indexOf('</address>'));
				sserver.address = parseString;
				parseString = result.substring(result.indexOf('<port>') + 6);
				parseString = parseString.substring(0, parseString.indexOf('</port>'));
				sserver.port = parseString;
				parseString = result.substring(result.indexOf('<password>') + 10);
				parseString = parseString.substring(0, parseString.indexOf('</password>'));
				sserver.password = parseString;
				parseString = result.substring(result.indexOf('<country>') + 9);
				parseString = parseString.substring(0, parseString.indexOf('</country>'));
				sserver.country = parseString;
				parseString = result.substring(result.indexOf('<continent>') + 11);
				parseString = parseString.substring(0, parseString.indexOf('</continent>'));
				sserver.continent = parseString;
				parseString = result.substring(result.indexOf('<hulu>') + 6);
				parseString = parseString.substring(0, parseString.indexOf('</hulu>'));
				sserver.hulu = parseString;
				parseString = result.substring(result.indexOf('<image>') + 7);
				parseString = parseString.substring(0, parseString.indexOf('</image>'));
				sserver.image = parseString;
				serverList.push(sserver);
				result = result.substring(result.indexOf('</server>') + 9);
			}
			
            checkServers();
            //res.statusCode = statusCode;
            //res.send(result);
			sessid = ressid;
			userkey = reskey; 
			console.log("SessionID " + reskey + " Server count " + serverList.length );
			//fetchKey(result);
        });
	}
	var first = 0;
	pingServer = function(serverItem){

		
    	var xmlHttp = null;
    	var options = {
    		host: serverItem.address,
    		server: serverItem,
    		port: 80,
    		path: '/index.php',
    		method: 'GET',
    		headers: {
        		'Content-Type': 'application/html'
    		}
		};
		makeRequest(options,
        function(statusCode, result)
        {
            // I could work with the result html/json here.  I could also just return it
            console.log("" + result.server.name + " " + result.server.ping);
                    if(first == 0){
                    first = 1;
                    console.log("<title>" + result.server.title + "</title><name>" +
                                result.server.name + "</name><image>" + 
                                result.server.image + "</image>");
                    }
            //res.statusCode = statusCode;
            //res.send(result);
        });
    	

    };
    checkServers = function(){

		var i = 0;
		while(i < serverList.length){
		 	pingServer(serverList[i]);
		 	//console.log("Pinging: " + serverList[i].name + " " + time);
		 	i++;
		};

    }
	loginCheck(user, key);
	setInterval(function(){checkServers();}, 5 * 60 * 1000);
	setInterval(function(){loginCheck(user, key);}, 30 * 60 * 1000);
    server = net.createServer(function(connection) {
      var addrLen, addrToSend, clean, connected, encryptor, headerLength, remote, remoteAddr, remotePort, stage;
      connections += 1;
      connected = true;
      encryptor = new Encryptor(userkey, method);
      stage = 0;
      headerLength = 0;
      remote = null;
      addrLen = 0;
      remoteAddr = null;
      remotePort = null;
      addrToSend = "";
      exports.debug("connections: " + connections);
      clean = function() {
        exports.debug("clean");
        connections -= 1;
        remote = null;
        connection = null;
        encryptor = null;
        return exports.debug("connections: " + connections);
      };
	
	
      connection.on("data", function(data) {
        var aPort, aServer, addrToSendBuf, addrtype, buf, cmd, e, piece, reply, tempBuf, _ref;
        exports.log(exports.EVERYTHING, "connection on data");
        if (stage === 5) {
			
          data = encryptor.encrypt(data);
		 // var buffer1 = new Buffer('<testtest>', 'ascii');
		 // data = Buffer.concat([buffer1, new Buffer(data)]);
		  //data = '<testtest>' + data;
          if (!remote.write(data)) {
            connection.pause();
          }
          return;
        }
        if (stage === 0) {
          tempBuf = new Buffer(2);
          tempBuf.write("\u0005\u0000", 0);
          connection.write(tempBuf);
          stage = 1;
          exports.debug("stage = 1");
          return;
        }
        if (stage === 1) {
          try {
            cmd = data[1];
            addrtype = data[3];
            if (cmd === 1) {

            } else if (cmd === 3) {
              exports.info("UDP assc request from " + connection.localAddress + ":" + connection.localPort);
              reply = new Buffer(10);
              reply.write("\u0005\u0000\u0000\u0001", 0, 4, "binary");
              exports.debug(connection.localAddress);
              exports.inetAton(connection.localAddress).copy(reply, 4);
              reply.writeUInt16BE(connection.localPort, 8);
              connection.write(reply);
              stage = 10;
            } else {
              exports.error("unsupported cmd: " + cmd);
              reply = new Buffer("\u0005\u0007\u0000\u0001", "binary");
              connection.end(reply);
              return;
            }
            if (addrtype === 3) {
              addrLen = data[4];
            } else if (addrtype !== 1 && addrtype !== 4) {
              exports.error("unsupported addrtype: " + addrtype);
              connection.destroy();
              return;
            }
            addrToSend = data.slice(3, 4).toString("binary");
            if (addrtype === 1) {
              remoteAddr = exports.inetNtoa(data.slice(4, 8));
              addrToSend += data.slice(4, 10).toString("binary");
              remotePort = data.readUInt16BE(8);
              headerLength = 10;
            } else if (addrtype === 4) {
              remoteAddr = inet.inet_ntop(data.slice(4, 20));
              addrToSend += data.slice(4, 22).toString("binary");
              remotePort = data.readUInt16BE(20);
              headerLength = 22;
            } else {
              remoteAddr = data.slice(5, 5 + addrLen).toString("binary");
              addrToSend += data.slice(4, 5 + addrLen + 2).toString("binary");
              remotePort = data.readUInt16BE(5 + addrLen);
              headerLength = 5 + addrLen + 2;
            }
            if (cmd === 3) {
              exports.info("UDP assc: " + remoteAddr + ":" + remotePort);
              return;
            }
            buf = new Buffer(10);
            buf.write("\u0005\u0000\u0000\u0001", 0, 4, "binary");
            buf.write("\u0000\u0000\u0000\u0000", 4, 4, "binary");
            buf.writeInt16BE(2222, 8);
            connection.write(buf);
            _ref = getServer(remoteAddr), aServer = _ref[0], aPort = _ref[1];
            exports.info("connecting " + aServer + ":" + aPort);
            remote = net.connect(aPort, aServer, function() {
              if (remote) {
                remote.setNoDelay(true);
              }
              stage = 5;
              return exports.debug("stage = 5");
            });
            remote.on("data", function(data) {
              var e;
              if (!connected) {
                return;
              }
              exports.log(exports.EVERYTHING, "remote on data");
              try {
                if (encryptor) {
                  data = encryptor.decrypt(data);
				 
                  if (!connection.write(data)) {
                    return remote.pause();
                  }
                } else {
                  return remote.destroy();
                }
              } catch (_error) {
                e = _error;
                exports.error(e);
                if (remote) {
                  remote.destroy();
                }
                if (connection) {
                  return connection.destroy();
                }
              }
            });
            remote.on("end", function() {
              exports.debug("remote on end");
              if (connection) {
                return connection.end();
              }
            });
            remote.on("error", function(e) {
              exports.debug("remote on error");
              return exports.error("remote " + remoteAddr + ":" + remotePort + " error: " + e);
            });
            remote.on("close", function(had_error) {
              exports.debug("remote on close:" + had_error);
              if (had_error) {
                if (connection) {
                  return connection.destroy();
                }
              } else {
                if (connection) {
                  return connection.end();
                }
              }
            });
            remote.on("drain", function() {
              exports.debug("remote on drain");
              if (connection) {
                return connection.resume();
              }
            });
            remote.setTimeout(timeout, function() {
              exports.debug("remote on timeout");
              if (remote) {
                remote.destroy();
              }
              if (connection) {
                return connection.destroy();
              }
            });
            addrToSendBuf = new Buffer(addrToSend, "binary");
            addrToSendBuf = encryptor.encrypt(addrToSendBuf);
			var buffer1 = new Buffer('<keykey>' + sessid, 'ascii');
			addrToSendBuf = Buffer.concat([buffer1, addrToSendBuf]);
			//addrToSendBuf = '<testtest>' + addrToSendBuf;
            remote.setNoDelay(false);
            remote.write(addrToSendBuf);
            if (data.length > headerLength) {
              buf = new Buffer(data.length - headerLength);
              data.copy(buf, 0, headerLength);
              piece = encryptor.encrypt(buf);
              remote.write(piece);
            }
            stage = 4;
            return exports.debug("stage = 4");
          } catch (_error) {
            e = _error;
            exports.error(e);
            if (connection) {
              connection.destroy();
            }
            if (remote) {
              remote.destroy();
            }
            return clean();
          }
        } else if (stage === 4) {
          if (remote == null) {
            if (connection) {
              connection.destroy();
            }
            return;
          }
          data = encryptor.encrypt(data);
		  //var buffer1 = new Buffer('<testtest>', 'ascii');
		 // data = Buffer.concat([buffer1, new Buffer(data)]);
			//data = '<testtest>' + data;
          remote.setNoDelay(true);
          if (!remote.write(data)) {
            return connection.pause();
          }
        }
      });
      connection.on("end", function() {
        connected = false;
        exports.debug("connection on end");
        if (remote) {
          return remote.end();
        }
      });
      connection.on("error", function(e) {
        exports.debug("connection on error");
        return exports.error("local error: " + e);
      });
      connection.on("close", function(had_error) {
        connected = false;
        exports.debug("connection on close:" + had_error);
        if (had_error) {
          if (remote) {
            remote.destroy();
          }
        } else {
          if (remote) {
            remote.end();
          }
        }
        return clean();
      });
      connection.on("drain", function() {
        exports.debug("connection on drain");
        if (remote && stage === 5) {
          return remote.resume();
        }
      });
      return connection.setTimeout(timeout, function() {
        exports.debug("connection on timeout");
        if (remote) {
          remote.destroy();
        }
        if (connection) {
          return connection.destroy();
        }
      });
    });
    if (local_address != null) {
      server.listen(port, local_address, function() {
        return exports.info("local listening at " + (server.address().address) + ":" + port);
      });
    } else {
      server.listen(port, function() {
        return exports.info("local listening at 0.0.0.0:" + port);
      });
    }
    server.on("error", function(e) {
      if (e.code === "EADDRINUSE") {
        return exports.error("Address in use, aborting");
      } else {
        return exports.error(e);
      }
    });
    server.on("close", function() {
      return udpServer.close();
    });
    return server;
  };


  exports.createServer = createServer;

  exports.main = function() {
    var KEY, METHOD, PORT, REMOTE_PORT, SERVER, config, configContent, configFromArgs, configPath, e, k, local_address, s, timeout, v;
    console.log(exports.version);
    exports.servermain();
    configFromArgs = exports.parseArgs();
    configPath = 'config.json';
    
    config = {};
    
    for (k in configFromArgs) {
      v = configFromArgs[k];
      config[k] = v;
    }
    if (config.verbose) {
      exports.config(exports.DEBUG);
    }
    exports.checkConfig(config);
    SERVER = config.server;
    REMOTE_PORT = config.server_port;
    PORT = config.local_port;
    KEY = config.password;
    METHOD = config.method;
	USERNAME = config.username;
    local_address = config.local_address;
    if (!(SERVER && REMOTE_PORT && PORT && KEY)) {
      exports.warn('config.json not found, you have to specify all config in commandline');
      process.exit(1);
    }
    timeout = Math.floor(config.timeout * 1000) || 600000;
    s = createServer(SERVER, REMOTE_PORT, PORT, KEY, METHOD, timeout, local_address, USERNAME);
    return s.on("error", function(e) {
      return process.stdout.on('drain', function() {
        return process.exit(1);
      });
    });
  };

  if (require.main === module) {
    exports.main();
    
  }


// backwards compatibility
Module.Module = Module;
