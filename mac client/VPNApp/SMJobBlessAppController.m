
#import <ServiceManagement/ServiceManagement.h>
#import <Security/Authorization.h>
#import "SMJobBlessAppController.h"

xpc_connection_t connection;
#define ANIM_NONE    0
#define ANIM_STARTED 1
#define ANIM_STOPPED 2
@interface SMJobBlessAppController ()

@property (nonatomic, assign) IBOutlet NSTextField* textField;

- (BOOL)blessHelperWithLabel:(NSString *)label error:(NSError **)error;

@end


@implementation SMJobBlessAppController
@synthesize usernameField;
@synthesize passwordField;
@synthesize rememberCheck;
@synthesize loginProgress;
@synthesize errorLabel;
@synthesize errorL;
@synthesize imgBack;
@synthesize textField=_textField;

- (void)appendLog:(NSString *)log {
    self.textField.stringValue = [self.textField.stringValue stringByAppendingFormat:@"\n%@", log];
}
BOOL isSingleServer = NO;
BOOL shouldterm = NO;
BOOL isFirst = NO;
BOOL isConnected = NO;
/*- (IBAction)testbutton:(NSButton *)sender {
    [self appendLog:@"Button press."];
    // xpc_connection_resume(connection);
    if(![[NSFileManager defaultManager] fileExistsAtPath:@"/Library/Frameworks/Mono.framework/Versions/3.4.0/etc/mono/config"])
    {
        isFirst = YES;
    }
    xpc_object_t message = xpc_dictionary_create(NULL, NULL, 0);
    NSString *myString = @"sudo ditto -xk ";
    NSString *exefiln = [[[NSBundle mainBundle] resourcePath]
                         stringByAppendingPathComponent:@"Versions.zip /Library/Frameworks/Mono.framework"];
    NSString *testn = [myString stringByAppendingString:exefiln];
    const char* txx = [testn cStringUsingEncoding:NSUTF8StringEncoding];
  //  const char* request = "Set Proxy";
    xpc_dictionary_set_string(message, "request", txx);

    NSAlert *alert = [[NSAlert alloc] init];
    [alert addButtonWithTitle:@"OK"];
    [alert addButtonWithTitle:@"Cancel"];
    [alert setMessageText:@"This App requires the Mono framework to run."];
    [alert setInformativeText:@"When clicking OK the download will start. Then you need to install it"];
    [alert setAlertStyle:NSWarningAlertStyle];

    xpc_connection_send_message_with_reply(connection, message, dispatch_get_main_queue(), ^(xpc_object_t event) {
        const char* response = xpc_dictionary_get_string(event, "reply");
        [self appendLog:[NSString stringWithFormat:@"Received response: %s.", response]];
    });
    [[NSApp keyWindow] close];
    
    
}*/

- (IBAction)testbutton:(NSButton *)sender {
    if([sender.title rangeOfString:@"Auto"].location != NSNotFound){
        isSingleServer = NO;
        if(isConnected){
            [btnChange setButtonOff];
            [self disconnecting];
        }
        [btnChange setButtonOn];
        [self connecting];
    }
    else{
    NSString *str = [[NSString alloc] initWithData:_responseData encoding:NSUTF8StringEncoding];
    if ([str rangeOfString:sender.title].location == NSNotFound) {
    } else {
        NSRange match;
        NSRange match1;
        match = [str rangeOfString: sender.title];
       
        int location = match.location - 50;
        if (location > 0) {
            str = [str substringWithRange: NSMakeRange (match.location-50, str.length - (match.location-50))];
        }
        
        
        
        match = [str rangeOfString: @"<title>"];
        match1 = [str rangeOfString: @"</title>"];
        osTitle = [[NSString alloc] init];
        osTitle = [[str substringWithRange: NSMakeRange (match.location+7, match1.location - (match.location+7))] retain];
        
        match = [str rangeOfString: @"<name>"];
        match1 = [str rangeOfString: @"</name>"];
        NSString *serverName = [str substringWithRange: NSMakeRange (match.location+6, match1.location - (match.location+6))];
        osName = [[NSString alloc] init];
        osName = [[@"Connected to " stringByAppendingString:serverName] retain];
        
        match = [str rangeOfString: @"<image>"];
        match1 = [str rangeOfString: @"</image>"];
        osImage = [[NSString alloc] init];
        osImage = [[str substringWithRange: NSMakeRange (match.location+7, match1.location - (match.location+7))] retain];
        
        
        match = [str rangeOfString: @"<address>"];
        match1 = [str rangeOfString: @"</address>"];
        osServer = [[NSString alloc] init];
        osServer = [[str substringWithRange: NSMakeRange (match.location+9, match1.location - (match.location+9))] retain];
        
        
        match = [str rangeOfString: @"<port>"];
        match1 = [str rangeOfString: @"</port>"];
        osPort = [[NSString alloc] init];
        osPort = [[str substringWithRange: NSMakeRange (match.location+6, match1.location - (match.location+6))] retain];
        
        isSingleServer = YES;
        if(isConnected){
            [btnChange setButtonOff];
            [self disconnecting];
        }
         [btnChange setButtonOn];
        [self connecting];
    }
    }
}
- (void) taskDidTerminate:(NSNotification *)notification {
    // Note this is called from the background thread, don't update the UI here
    shouldterm = YES;
    
  /*
        
        [[NSNotificationCenter defaultCenter] addObserver:self
                                                 selector:@selector(taskDidTerminate:)
                                                     name:NSTaskDidTerminateNotification
                                                   object:nil];
        
    }
    else{*/
    NSLog(@"end");
       //}
    
    // Call updateUI method on main thread to update the user interface
//    [self performSelectorOnMainThread:@selector(updateUI) withObject:nil waitUntilDone:NO];
}



- (void) connectVPN{
    //[writer writeData:[@"Connect\n" dataUsingEncoding:NSUTF8StringEncoding]];
    
    xpc_object_t message = xpc_dictionary_create(NULL, NULL, 0);
    const char* request = "Set Proxy";
    xpc_dictionary_set_string(message, "request", request);
    
    xpc_connection_send_message_with_reply(connection, message, dispatch_get_main_queue(), ^(xpc_object_t event) {
        const char* response = xpc_dictionary_get_string(event, "reply");
        NSLog(@"Received response:"
              );
    });
    //[writer synchronizeFile];
}

- (void) disconnectVPN{
    //[writer writeData:[@"Disconnect\n" dataUsingEncoding:NSUTF8StringEncoding]];
    xpc_object_t message = xpc_dictionary_create(NULL, NULL, 0);
    const char* request = "Remove Proxy";
    xpc_dictionary_set_string(message, "request", request);
    
    [self appendLog:[NSString stringWithFormat:@"Sending request: %s", request]];
    
    xpc_connection_send_message_with_reply(connection, message, dispatch_get_main_queue(), ^(xpc_object_t event) {
        const char* response = xpc_dictionary_get_string(event, "reply");
        [self appendLog:[NSString stringWithFormat:@"Received response: %s.", response]];
        
    });
    //[writer synchronizeFile];
}

- (void) quitVPN{
    //[writer writeData:[@"Quit\n" dataUsingEncoding:NSUTF8StringEncoding]];
    xpc_object_t message = xpc_dictionary_create(NULL, NULL, 0);
    const char* request = "Remove Proxy";
    xpc_dictionary_set_string(message, "request", request);
    
    [self appendLog:[NSString stringWithFormat:@"Sending request: %s", request]];
    if(connection != nil){
    xpc_connection_send_message_with_reply(connection, message, dispatch_get_main_queue(), ^(xpc_object_t event) {
        const char* response = xpc_dictionary_get_string(event, "reply");
        [self appendLog:[NSString stringWithFormat:@"Received response: %s.", response]];
    
    });
    }
    //[writer synchronizeFile];
}
- (void)windowWillClose:(NSNotification *)notification
{
    [self disconnectVPN];
    [self quitVPN];
}

-(void)closeThisWindow {
    
    //
    // The NSWindowCloseButton has been clicked.
    // Code to be run before the window closes.
    //
    [self disconnectVPN];
     [self quitVPN];
     [self.window close];
}

- (void)stdoutDataAvailable:(NSNotification *)notification
{
    NSFileHandle *handle = (NSFileHandle *)[notification object];
    NSData *inData = nil;
    if (isSingleServer == NO) {
    if ((inData = [[notification userInfo] objectForKey:@"NSFileHandleNotificationDataItem"])) {
        NSString *str = [[NSString alloc] initWithData:inData encoding:NSUTF8StringEncoding];
         NSLog(str);
        if ([str rangeOfString:@"EADDRINUSE"].location != NSNotFound) {
            [self connecting];
        }
        if ([str rangeOfString:@"<title>"].location == NSNotFound) {
        } else {
            NSRange match;
            NSRange match1;
            match = [str rangeOfString: @"<title>"];
            match1 = [str rangeOfString: @"</title>"];
            titleString = [str substringWithRange: NSMakeRange (match.location+7, match1.location - (match.location+7))];
            
            match = [str rangeOfString: @"<name>"];
            match1 = [str rangeOfString: @"</name>"];
            NSString *serverName = [str substringWithRange: NSMakeRange (match.location+6, match1.location - (match.location+6))];
            serverString = [@"Connected to " stringByAppendingString:serverName];
            
            match = [str rangeOfString: @"<image>"];
            match1 = [str rangeOfString: @"</image>"];
            NSString *imageName = [str substringWithRange: NSMakeRange (match.location+7, match1.location - (match.location+7))];
            [txtTitle setStringValue:titleString];
            [txtServer setStringValue:serverString];
            
            cityImage = [NSImage imageNamed:imageName];
            [self changeBackground: imageView2];
            isConnected = YES;
            //[self initAnim];
            [anim startAnimation];
        }
    }
    }
   //
    [handle readInBackgroundAndNotify] ;

    //if([str conta]
        
        // [handle waitForDataInBackgroundAndNotify];
    // consoleView is an NSTextView
   // [self.consoleView setString:[[self.consoleView string] stringByAppendingFormat:@"Output:\n%@", str]];
}

-(void)applicationWillTerminate:(NSNotification *)notification
{
    //[self disconnectVPN];
    [self quitVPN];
}

- (void)connection:(NSURLConnection *)connection didReceiveResponse:(NSURLResponse *)response {
    // A response has been received, this is where we initialize the instance var you created
    // so that we can append data to it in the didReceiveData method
    // Furthermore, this method is called each time there is a redirect so reinitializing it
    // also serves to clear it
    _responseData = [[NSMutableData alloc] init];
}

- (void)connection:(NSURLConnection *)connection didReceiveData:(NSData *)data {
    // Append the new data to the instance variable you declared
    [_responseData appendData:data];
}

- (NSCachedURLResponse *)connection:(NSURLConnection *)connection
                  willCacheResponse:(NSCachedURLResponse*)cachedResponse {
    // Return nil to indicate not necessary to store a cached response for this connection
    return nil;
}
- (void)connection:(NSURLConnection *)connection didFailWithError:(NSError *)error {
    
    [self connectRetry];
    
}
- (void)connectionDidFinishLoading:(NSURLConnection *)connection {
    NSString *xmlstr = [[NSString alloc] initWithData:_responseData encoding:NSUTF8StringEncoding];
    if ([xmlstr rangeOfString:@"<msg>Success</msg>"].location != NSNotFound) {
        // The request is complete and data has been received
        // You can parse the stuff in your instance variable now
        NSRange mz;
        NSRange mz1;
        mz = [xmlstr rangeOfString: @"<hash>"];
        mz1 = [xmlstr rangeOfString: @"</hash>"];
        hashString = [[NSString alloc] init];
        hashString = [[xmlstr substringWithRange: NSMakeRange (mz.location+6, mz1.location - (mz.location+6))] retain];
        NSString *ssu = usernameField.stringValue;
        NSString *ssp = passwordField.stringValue;
        userString = [usernameField.stringValue retain];
        if ([rememberCheck state] == NSOnState) {
            [[NSUserDefaults standardUserDefaults] setValue:ssu forKey:@"email"];
            [[NSUserDefaults standardUserDefaults] setValue:ssp forKey:@"password"];
        }
        else{
            [[NSUserDefaults standardUserDefaults] setValue:@"" forKey:@"email"];
            [[NSUserDefaults standardUserDefaults] setValue:@"" forKey:@"password"];
        }
        [[NSUserDefaults standardUserDefaults] synchronize];
        [win orderFront:0];
        [loginWindow close];
        NSMenu *mainMenu =
        [[NSApplication sharedApplication] mainMenu];
        NSMenu *appMenu = [[mainMenu itemAtIndex:2] submenu];
        [appMenu removeItemAtIndex:0];
        NSMenuItem *item = [[NSMenuItem alloc] initWithTitle:@"Auto"
                                                      action:@selector(testbutton:) keyEquivalent:@""];
        
        [item autorelease];
        [item setTarget:self];
        [appMenu addItem:item];
        while ([xmlstr rangeOfString:@"<name>"].location != NSNotFound) {
            
            NSRange match;
            NSRange match1;
            match = [xmlstr rangeOfString: @"<name>"];
            match1 = [xmlstr rangeOfString: @"</name>"];
            NSString *tString = [xmlstr substringWithRange: NSMakeRange (match.location+6, match1.location - (match.location+6))];
            xmlstr = [xmlstr substringWithRange: NSMakeRange (match1.location+6, xmlstr.length - (match1.location+6))];
            
            NSMenuItem *item = [[NSMenuItem alloc] initWithTitle:tString
                                                          action:@selector(testbutton:) keyEquivalent:@""];
            
            [item autorelease];
            [item setTarget:self];
            [appMenu addItem:item];
        }
    }
    else{
        [loginProgress stopAnimation:0];
        [errorL setHidden:NO];
        [[NSUserDefaults standardUserDefaults] setValue:@"" forKey:@"email"];
        [[NSUserDefaults standardUserDefaults] setValue:@"" forKey:@"password"];
        [[NSUserDefaults standardUserDefaults] synchronize];
    }
    
}

- (IBAction)loginClicked:(id)sender {
    win = [self window];
    animState = ANIM_NONE;
    titleString = @"Disconnected";
    serverString = @"Not connected to any server";
    // NSButton *closeButton = [self.window standardWindowButton:NSWindowCloseButton];
    //[closeButton setTarget:self.window.delegate];
    //[closeButton setAction:@selector(closeThisWindow)];
    //[closeButton setEnabled:YES];
    oldbits = 0;
    cityImage = [NSImage imageNamed:@"gui3.png"];
    [self initImages];
    [self initImageViewWithWindow:win];
    [self changeBackground: imageView];
    
    [self initButton];
    [self initText];
    [self initThread];
    [self initAnim];
    
    // redirect stdin to input pipe file handle
    // dup2([[inputPipe fileHandleForWriting] fileDescriptor], STDIN_FILENO);
    // curInputHandle is an instance variable of type NSFileHandle
    
    
    
    
    [self.window setLevel:NSScreenSaverWindowLevel];
    NSString *ssu = usernameField.stringValue;
    NSString *ssp = passwordField.stringValue;
    NSString *os =  @"Windows";
    NSString *major =  @"1";
    NSString *minor =  @"0";
    [errorL setHidden:YES];
    [loginProgress startAnimation:0];
    NSString *post = [NSString stringWithFormat:@"username=%@&password=%@&os=%@&major=%@&minor=%@",ssu,ssp,os,major,minor];
	NSData *postData = [post dataUsingEncoding:NSASCIIStringEncoding allowLossyConversion:YES];
    NSString *postLength = [NSString stringWithFormat:@"%lu",(unsigned long)[postData length]];
    NSMutableURLRequest *request = [[[NSMutableURLRequest alloc] init] autorelease];
    [request setURL:[NSURL URLWithString:[NSString stringWithFormat:@"https://157.7.234.46/api/User/Login"]]];
    [request setHTTPMethod:@"POST"];
    [request setValue:postLength forHTTPHeaderField:@"Content-Length"];
    [request setValue:@"application/x-www-form-urlencoded" forHTTPHeaderField:@"Content-Type"];
    [request setHTTPBody:postData];
    
    NSURLConnection *conn = [[NSURLConnection alloc]initWithRequest:request delegate:self];
    if(conn) {
        NSLog(@"Connection Successful");
    } else {
        NSLog(@"Connection could not be made");
    }
}

- (void) connectRetry{
    
    NSString *ssu = usernameField.stringValue;
    NSString *ssp = passwordField.stringValue;
    NSString *os =  @"Windows";
    NSString *major =  @"1";
    NSString *minor =  @"0";
    [errorL setHidden:YES];
    [loginProgress startAnimation:0];
    NSString *post = [NSString stringWithFormat:@"username=%@&password=%@&os=%@&major=%@&minor=%@",ssu,ssp,os,major,minor];
	NSData *postData = [post dataUsingEncoding:NSASCIIStringEncoding allowLossyConversion:YES];
    NSString *postLength = [NSString stringWithFormat:@"%lu",(unsigned long)[postData length]];
    NSMutableURLRequest *request = [[[NSMutableURLRequest alloc] init] autorelease];
    [request setURL:[NSURL URLWithString:[NSString stringWithFormat:@"https://157.7.194.214/api/User/Login"]]];
    [request setHTTPMethod:@"POST"];
    [request setValue:postLength forHTTPHeaderField:@"Content-Length"];
    [request setValue:@"application/x-www-form-urlencoded" forHTTPHeaderField:@"Content-Type"];
    [request setHTTPBody:postData];
    
    NSURLConnection *conn = [[NSURLConnection alloc]initWithRequest:request delegate:self];
    if(conn) {
        NSLog(@"Connection Successful");
    } else {
        NSLog(@"Connection could not be made");
    }
}



- (void)applicationDidFinishLaunching:(NSNotification *)notification {
    [errorL setHidden:YES];
    [self connectHelper];
    [self disconnectVPN];
    NSString *_email = [[NSUserDefaults standardUserDefaults] stringForKey:@"email"];
    NSString *_password = [[NSUserDefaults standardUserDefaults] stringForKey:@"password"];
    if(_email && _password){
        [usernameField setStringValue:_email];
        [passwordField setStringValue:_password];
    }
}

- (BOOL) applicationShouldTerminateAfterLastWindowClosed:(NSApplication *)application
{
    return YES;
}
- (BOOL)blessHelperWithLabel:(NSString *)label
                       error:(NSError **)error {
    
	BOOL result = NO;

	AuthorizationItem authItem		= { kSMRightBlessPrivilegedHelper, 0, NULL, 0 };
	AuthorizationRights authRights	= { 1, &authItem };
	AuthorizationFlags flags		=	kAuthorizationFlagDefaults				| 
										kAuthorizationFlagInteractionAllowed	|
										kAuthorizationFlagPreAuthorize			|
										kAuthorizationFlagExtendRights;

	AuthorizationRef authRef = NULL;
	
	/* Obtain the right to install privileged helper tools (kSMRightBlessPrivilegedHelper). */
	OSStatus status = AuthorizationCreate(&authRights, kAuthorizationEmptyEnvironment, flags, &authRef);
	if (status != errAuthorizationSuccess) {
        [self appendLog:[NSString stringWithFormat:@"Failed to create AuthorizationRef. Error code: %ld", status]];
        
	} else {
		/* This does all the work of verifying the helper tool against the application
		 * and vice-versa. Once verification has passed, the embedded launchd.plist
		 * is extracted and placed in /Library/LaunchDaemons and then loaded. The
		 * executable is placed in /Library/PrivilegedHelperTools.
		 */
		result = SMJobBless(kSMDomainSystemLaunchd, (CFStringRef)label, authRef, (CFErrorRef *)error);
	}
	
	return result;
}

- (void) initImages
{
    NSImage *img;
    iImageNum = 0;
    iImageCount = 0;
    
    self.imgBack = [NSMutableArray arrayWithCapacity:5];
    
    img = [NSImage imageNamed:@"gui3.png"];
    [self.imgBack addObject:	img];
    iImageCount++;
    
    img = [NSImage imageNamed:@"0.png"];
    [self.imgBack addObject:	img];
    iImageCount++;
    
    img = [NSImage imageNamed:@"2.png"];
    [self.imgBack addObject:	img];
    iImageCount++;
    
    img = [NSImage imageNamed:@"3.png"];
    [self.imgBack addObject:	img];
    iImageCount++;
    
    img = [NSImage imageNamed:@"4.png"];
    [self.imgBack addObject:	img];
    iImageCount++;
    
    img = [NSImage imageNamed:@"5.png"];
    [self.imgBack addObject:	img];
    iImageCount++;}

- (void) initImageViewWithWindow:(NSWindow*) win
{
    NSView *view = [win contentView];
    
    imageView = [[NSImageView alloc] initWithFrame:view.bounds];
    [imageView setAutoresizingMask: (NSViewNotSizable)]; // | NSViewHeightSizable)];
    [imageView setImageScaling: NSScaleNone];
    
    imageView2 = [[NSImageView alloc] initWithFrame:view.bounds];
    [imageView setAutoresizingMask: (NSViewNotSizable)]; // | NSViewHeightSizable)];
    [imageView setImageScaling: NSScaleNone];
    
    [view addSubview: imageView];
    [view addSubview: imageView2];
}

- (void)changeBackground:(NSImageView*) view;
{
    [view setImage: cityImage];
}

- (void)changeText
{
    /*NSDate *now = [NSDate date];
     NSCalendar *calendar = [NSCalendar currentCalendar];
     NSDateComponents *components = [calendar components:(NSHourCalendarUnit | NSMinuteCalendarUnit | NSSecondCalendarUnit) fromDate:now];
     int hour = (int)[components hour];
     int minute = (int)[components minute];
     int second = (int)[components second];*/
    
 //   [txtTitle setStringValue:titleString];
  //  [txtServer setStringValue:serverString];
  
    
    u_int64_t tbits = getBytes();
    u_int64_t bitdiff = (tbits - oldbits) * 8 / 1000;
    oldbits = tbits;
    if (isConnected == NO) {
        NSString *str = [NSString stringWithFormat:@" "];
        [txtDesc setStringValue:str];

    }
    else {
        NSString *str = [NSString stringWithFormat:@"Download Speed: %d kb/s", bitdiff];
        [txtDesc setStringValue:str];
    }
    
   
     // [reader readInBackgroundAndNotify];
    // [win orderFrontRegardless];
}

- (void) initButton
{
    NSRect rect = CGRectMake(82, 122, 77, 78);
    btnChange = [[VButton alloc] initWithFrame:rect];
    [btnChange setBordered:NO];
    [btnChange setImage:[NSImage imageNamed:@"red3.png"]];
    //[btnChange setAlternateImage: [NSImage imageNamed:@"red3.png"]];
    [btnChange setButtonType:NSMomentaryChangeButton];
    [[btnChange cell] setImageScaling:NSImageScaleAxesIndependently];
    //[imageView addSubview:btnChange];
    [btnChange createTrackingArea];
    [[[self window] contentView] addSubview:btnChange];
    [btnChange setTarget:self];
    [btnChange setAction:@selector(changeClick:)];
}

- (NSImage*) getImage
{

    NSImage *img = [self.imgBack objectAtIndex:iImageNum];
    return img;
}

- (void)disconnecting
{
    @try {
        if(runTask != nil){
            if([runTask isRunning]){
                [runTask terminate];
            }
        }

    }
    
    @catch ( NSException *e ) {
        
    }
    
    @finally {
    }


    titleString = @"Disconnected";
    serverString = @"Not connected to any server";
    isConnected = NO;
    [txtTitle setStringValue:titleString];
    [txtServer setStringValue:serverString];
    cityImage = [NSImage imageNamed:@"gui3.png"];
    [self disconnectVPN];
    [self changeBackground: imageView2];
    
    //[self initAnim];
    [anim startAnimation];
}

- (void)connectHelper
{
    NSError *error = nil;
    if([[NSFileManager defaultManager] fileExistsAtPath:@"/Library/PrivilegedHelperTools/com.kantaris.vpn.VPNHelper"]) { /* ... */ }
    else{
        NSAlert *alert = [[NSAlert alloc] init];
        [alert addButtonWithTitle:@"OK"];
        //[alert addButtonWithTitle:@"Cancel"];
        [alert setMessageText:@"This App requires a helper to run"];
        [alert setInformativeText:@"When clicking OK the helper will be installed"];
        [alert setAlertStyle:NSWarningAlertStyle];
        [alert runModal];
        if (![self blessHelperWithLabel:@"com.kantaris.vpn.VPNHelper" error:&error]) {
            [self appendLog:[NSString stringWithFormat:@"Failed to bless helper. Error: %@", error]];
            return;
        }
    }
    self.textField.stringValue = @"Helper available.";
    if(connection == nil){
        connection = xpc_connection_create_mach_service("com.kantaris.vpn.VPNHelper", NULL, XPC_CONNECTION_MACH_SERVICE_PRIVILEGED);
        
        if (!connection) {
            [self appendLog:@"Failed to create XPC connection."];
            return;
        }
        
        xpc_connection_set_event_handler(connection, ^(xpc_object_t event) {
            xpc_type_t type = xpc_get_type(event);
            
            if (type == XPC_TYPE_ERROR) {
                
                if (event == XPC_ERROR_CONNECTION_INTERRUPTED) {
                    [self appendLog:@"XPC connection interupted."];
                    
                } else if (event == XPC_ERROR_CONNECTION_INVALID) {
                    [self appendLog:@"XPC connection invalid, releasing."];
                    xpc_release(connection);
                    
                } else {
                    [self appendLog:@"Unexpected XPC connection error."];
                }
                
            } else {
                [self appendLog:@"Unexpected XPC connection event."];
            }
        });
        
        xpc_connection_resume(connection);
    }
}

- (void)connecting
{
    NSPipe *outputPipe = [NSPipe pipe];
    reader = [outputPipe fileHandleForReading];
    [reader readInBackgroundAndNotify];
    // when my C program hits a scanf
    NSPipe *pipe = [[NSPipe alloc] init];
    writer = [pipe fileHandleForWriting];
    
    
    runTask = [[[NSTask alloc] init] autorelease];
    NSString *exefile = [[[NSBundle mainBundle] resourcePath]
                         stringByAppendingPathComponent:@"vpncore"];
    [runTask setLaunchPath: exefile];
    
    NSString *exeDir = [[[NSBundle mainBundle] resourcePath]
                        stringByAppendingPathComponent:@"alpha"];
    NSArray *pargs;
    int rand = 1000 + arc4random_uniform(1000);
    NSString *myT = [NSString stringWithFormat:@"%d", rand];
    if(isSingleServer == NO){
        pargs = [NSArray arrayWithObjects: exeDir, @"-s", @"Auto" ,@"-p", @"443", @"-l", @"1179", @"-u", userString,@"-k", hashString, @"-m", @"aes-256-cfb", nil];
    }
    else{
        pargs = [NSArray arrayWithObjects: exeDir, @"-s", osServer ,@"-p", osPort, @"-l", @"1179", @"-u", userString,@"-k", hashString, @"-m", @"aes-256-cfb", nil];
    }
    [runTask setArguments: pargs];
    [runTask setStandardInput:pipe];
    [runTask setStandardOutput:outputPipe];
    [runTask setStandardError:outputPipe];
    NSNotificationCenter *center = [NSNotificationCenter defaultCenter];
    [center addObserver: self selector:@selector(stdoutDataAvailable:) name:NSFileHandleReadCompletionNotification object:reader];
    [runTask launch];
    
    
    [self connectHelper];
    if(isSingleServer){
        [txtTitle setStringValue:osTitle];
        [txtServer setStringValue:osName];
        cityImage = [NSImage imageNamed:osImage];
        [self changeBackground: imageView2];
        isConnected = YES;
        //[self initAnim];
        [anim startAnimation];
    }
    else{
        titleString = @"Connecting";
        [txtTitle setStringValue:titleString];
    }
    [self connectVPN];
}

- (IBAction) changeClick : (id) sender
{
    //[btnChange setImage:[NSImage imageNamed:@"green3.png"]];
    //[btnChange setAlternateImage: [NSImage imageNamed:@"green3.png"]];
    if(animState == ANIM_STARTED) return;
    iImageNum += 1;
    iImageNum %= iImageCount;
    
    
    
    if(!isConnected){
        [self connecting];
    }
    else{
        [self disconnecting];
        
    
        }
    }
    

u_int64_t getBytes(){
    int mib[] = {
        CTL_NET,
        PF_ROUTE,
        0,
        0,
        NET_RT_IFLIST2,
        0
    };
    size_t len;
    if (sysctl(mib, 6, NULL, &len, NULL, 0) < 0) {
        fprintf(stderr, "sysctl: %s\n", strerror(errno));
        exit(1);
    }
    char *buf = (char *)malloc(len);
    if (sysctl(mib, 6, buf, &len, NULL, 0) < 0) {
        fprintf(stderr, "sysctl: %s\n", strerror(errno));
        exit(1);
    }
    char *lim = buf + len;
    char *next = NULL;
    u_int64_t totalibytes = 0;
    u_int64_t totalobytes = 0;
    for (next = buf; next < lim; ) {
        struct if_msghdr *ifm = (struct if_msghdr *)next;
        next += ifm->ifm_msglen;
        if (ifm->ifm_type == RTM_IFINFO2) {
            struct if_msghdr2 *if2m = (struct if_msghdr2 *)ifm;
            totalibytes += if2m->ifm_data.ifi_ibytes;
            totalobytes += if2m->ifm_data.ifi_obytes;
        }
    }
    return totalobytes;
}
- (void) initText
{
    NSColor *myColor = [NSColor colorWithCalibratedRed:0.5f green:0.5f blue:0.5f alpha:1.0f];
    NSColor *titleColor = [NSColor colorWithCalibratedRed:0.283f green:0.223f blue:0.4f alpha:1.0f];
    txtDesc = [[NSTextField alloc] initWithFrame:NSMakeRect(0, 00, 200, 20)];
    [txtDesc setAlignment:NSCenterTextAlignment];
    [txtDesc setStringValue:@"My Label"];
    [txtDesc setFont:[NSFont fontWithName:@"Arial" size:11]];
    [txtDesc setBezeled:NO];
    [txtDesc setTextColor: myColor];
    [txtDesc setDrawsBackground:NO];
    [txtDesc setEditable:NO];
    [txtDesc setSelectable:NO];
    [txtDesc setFrameOrigin:NSMakePoint(
                                        (NSWidth([imageView bounds]) - NSWidth([txtDesc frame])) / 2,
                                        38
                                        )];
    [txtDesc setAutoresizingMask:NSViewMinXMargin | NSViewMaxXMargin | NSViewMinYMargin | NSViewMaxYMargin];
    
    //[imageView addSubview:txtDesc];
    [[[self window] contentView] addSubview:txtDesc];
    
    txtServer = [[NSTextField alloc] initWithFrame:NSMakeRect(0, 00, 200, 20)];
    [txtServer setAlignment:NSCenterTextAlignment];
    [txtServer setStringValue:serverString];
    [txtServer setFont:[NSFont fontWithName:@"Arial" size:11]];
    [txtServer setBezeled:NO];
    [txtServer setTextColor: myColor];
    [txtServer setDrawsBackground:NO];
    [txtServer setEditable:NO];
    [txtServer setSelectable:NO];
    [txtServer setFrameOrigin:NSMakePoint(
                                          (NSWidth([imageView bounds]) - NSWidth([txtServer frame])) / 2,
                                          55
                                          )];
    [txtServer setAutoresizingMask:NSViewMinXMargin | NSViewMaxXMargin | NSViewMinYMargin | NSViewMaxYMargin];
    
    //[imageView addSubview:txtDesc];
    [[[self window] contentView] addSubview:txtServer];
    
    
    txtMode = [[NSTextField alloc] initWithFrame:NSMakeRect(0, 00, 200, 20)];
    [txtMode setAlignment:NSCenterTextAlignment];
    [txtMode setStringValue:@"Mode: OpenWeb"];
    [txtMode setFont:[NSFont fontWithName:@"Arial" size:11]];
    [txtMode setTextColor: myColor];
    [txtMode setBezeled:NO];
    [txtMode setDrawsBackground:NO];
    [txtMode setEditable:NO];
    [txtMode setSelectable:NO];
    [txtMode setFrameOrigin:NSMakePoint((NSWidth([imageView bounds]) - NSWidth([txtMode frame])) / 2,
                                        21
                                        )];
    [txtServer setAutoresizingMask:NSViewMinXMargin | NSViewMaxXMargin | NSViewMinYMargin | NSViewMaxYMargin];
    
    [[[self window] contentView] addSubview:txtMode];
    
    
    oldbits = getBytes();
    
    txtTitle = [[NSTextField alloc] initWithFrame:NSMakeRect(0, 00, 200, 40)];
    [txtTitle setAlignment:NSCenterTextAlignment];
    [txtTitle setStringValue:titleString];
    [txtTitle setFont:[NSFont fontWithName:@"Arial" size:23]];
    [txtTitle setTextColor: titleColor];
    [txtTitle setBezeled:NO];
    [txtTitle setDrawsBackground:NO];
    [txtTitle setEditable:NO];
    [txtTitle setSelectable:NO];
    [txtTitle setFrameOrigin:NSMakePoint((NSWidth([imageView bounds]) - NSWidth([txtTitle frame])) / 2,
                                         70
                                         )];
    [txtServer setAutoresizingMask:NSViewMinXMargin | NSViewMaxXMargin | NSViewMinYMargin | NSViewMaxYMargin];
    
    [[[self window] contentView] addSubview:txtTitle];
    
}

-(void) initThread
{
    thread = [[NSThread alloc] initWithTarget:self
                                     selector:@selector(counterMethod:)
                                       object:nil];
    cond = [[NSCondition alloc] init];
    [cond lock];
    [thread start];
    
}

- (void) counterMethod: (id)obj
{
    BOOL exitNow = NO;
    //NSRunLoop* runLoop = [NSRunLoop currentRunLoop];
    
    // Add the exitNow BOOL to the thread dictionary.
    NSMutableDictionary* threadDict = [[NSThread currentThread] threadDictionary];
    [threadDict setValue:[NSNumber numberWithBool:exitNow] forKey:@"ThreadShouldExitNow"];
    
    
    
    while (!exitNow)
    {
        [self changeText];
        [NSThread sleepForTimeInterval: 1];
        // Check to see if an input source handler changed the exitNow value.
        exitNow = [[threadDict valueForKey:@"ThreadShouldExitNow"] boolValue];
    }
    [cond signal];
}

- (void) initAnim
{
    animAttrs = [NSDictionary dictionaryWithObjectsAndKeys:
                 imageView2, NSViewAnimationTargetKey,
                 NSViewAnimationFadeInEffect, NSViewAnimationEffectKey,
                 nil];
    
    anim = [[NSViewAnimation alloc] initWithViewAnimations:[NSArray arrayWithObjects: animAttrs, nil]];
    
    [anim setDuration:1.2];
    [anim setAnimationCurve:NSAnimationEaseInOut];
    [anim setAnimationBlockingMode:NSAnimationBlocking];
    [anim setDelegate: self];
}


- (BOOL)animationShouldStart:(NSAnimation*) animation
{
    animState = ANIM_STARTED;
    return YES;
}

- (void)animationDidEnd:(NSAnimation *)animation
{
    animState = ANIM_STOPPED;
    
    [self changeBackground:imageView];
    [imageView2 setAlphaValue: 0];
}

- (BOOL)connection:(NSURLConnection *)connection canAuthenticateAgainstProtectionSpace:(NSURLProtectionSpace *)protectionSpace {
    return [protectionSpace.authenticationMethod isEqualToString:NSURLAuthenticationMethodServerTrust];
}

- (void)connection:(NSURLConnection *)connection didReceiveAuthenticationChallenge:(NSURLAuthenticationChallenge *)challenge {
    if ([challenge.protectionSpace.authenticationMethod isEqualToString:NSURLAuthenticationMethodServerTrust])
            [challenge.sender useCredential:[NSURLCredential credentialForTrust:challenge.protectionSpace.serverTrust] forAuthenticationChallenge:challenge];
    
    [challenge.sender continueWithoutCredentialForAuthenticationChallenge:challenge];
}
@end
