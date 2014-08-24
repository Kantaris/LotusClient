
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
@synthesize imgBack;
@synthesize textField=_textField;

- (void)appendLog:(NSString *)log {
    self.textField.stringValue = [self.textField.stringValue stringByAppendingFormat:@"\n%@", log];
}

BOOL shouldterm = NO;
BOOL isFirst = NO;
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
    [self quitVPN];
}

-(void)closeThisWindow {
    
    //
    // The NSWindowCloseButton has been clicked.
    // Code to be run before the window closes.
    //
     [self quitVPN];
     [self.window close];
}

- (void)stdoutDataAvailable:(NSNotification *)notification
{
    NSFileHandle *handle = (NSFileHandle *)[notification object];
    NSData *inData = nil;
    if ((inData = [[notification userInfo] objectForKey:@"NSFileHandleNotificationDataItem"])) {
        NSString *str = [[NSString alloc] initWithData:inData encoding:NSUTF8StringEncoding];
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
            
            //[self initAnim];
            [anim startAnimation];        }
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
    [self quitVPN];
}
- (void)applicationDidFinishLaunching:(NSNotification *)notification {
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
    NSString *str = [NSString stringWithFormat:@"Download Speed: %d kb/s", bitdiff];
    [txtDesc setStringValue:str];
    
   
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

- (IBAction) changeClick : (id) sender
{
    //[btnChange setImage:[NSImage imageNamed:@"green3.png"]];
    //[btnChange setAlternateImage: [NSImage imageNamed:@"green3.png"]];
    if(animState == ANIM_STARTED) return;
    iImageNum += 1;
    iImageNum %= iImageCount;
    
    
    
    if(btnChange.state == 1){
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
                            stringByAppendingPathComponent:@"local"];
        NSArray *pargs;
        pargs = [NSArray arrayWithObjects: exeDir, @"-s", @"00" ,@"-p", @"8388", @"-l", @"1179", @"-k", @"barfoo!", @"-m", @"aes-256-cfb", nil];
        [runTask setArguments: pargs];
        [runTask setStandardInput:pipe];
        [runTask setStandardOutput:outputPipe];
        [runTask setStandardError:outputPipe];
        [runTask launch];
        NSNotificationCenter *center = [NSNotificationCenter defaultCenter];
        [center addObserver: self selector:@selector(stdoutDataAvailable:) name:NSFileHandleReadCompletionNotification object:reader];
        
       
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
        [self connectVPN];
    }
    else{
        [runTask terminate];
        titleString = @"Disconnected";
        serverString = @"Not connected to any server";
        [txtTitle setStringValue:titleString];
        [txtServer setStringValue:serverString];
        cityImage = [NSImage imageNamed:@"gui3.png"];
        [self disconnectVPN];
        [self changeBackground: imageView2];
        
        //[self initAnim];
        [anim startAnimation];
        
    
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



@end
