/* 
 
     File: SMJobBlessAppController.h
 Abstract: The main application controller header.
  Version: 1.2
 
 Disclaimer: IMPORTANT:  This Apple software is supplied to you by Apple
 Inc. ("Apple") in consideration of your agreement to the following
 terms, and your use, installation, modification or redistribution of
 this Apple software constitutes acceptance of these terms.  If you do
 not agree with these terms, please do not use, install, modify or
 redistribute this Apple software.
 
 In consideration of your agreement to abide by the following terms, and
 subject to these terms, Apple grants you a personal, non-exclusive
 license, under Apple's copyrights in this original Apple software (the
 "Apple Software"), to use, reproduce, modify and redistribute the Apple
 Software, with or without modifications, in source and/or binary forms;
 provided that if you redistribute the Apple Software in its entirety and
 without modifications, you must retain this notice and the following
 text and disclaimers in all such redistributions of the Apple Software.
 Neither the name, trademarks, service marks or logos of Apple Inc. may
 be used to endorse or promote products derived from the Apple Software
 without specific prior written permission from Apple.  Except as
 expressly stated in this notice, no other rights or licenses, express or
 implied, are granted by Apple herein, including but not limited to any
 patent rights that may be infringed by your derivative works or by other
 works in which the Apple Software may be incorporated.
 
 The Apple Software is provided by Apple on an "AS IS" basis.  APPLE
 MAKES NO WARRANTIES, EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION
 THE IMPLIED WARRANTIES OF NON-INFRINGEMENT, MERCHANTABILITY AND FITNESS
 FOR A PARTICULAR PURPOSE, REGARDING THE APPLE SOFTWARE OR ITS USE AND
 OPERATION ALONE OR IN COMBINATION WITH YOUR PRODUCTS.
 
 IN NO EVENT SHALL APPLE BE LIABLE FOR ANY SPECIAL, INDIRECT, INCIDENTAL
 OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 INTERRUPTION) ARISING IN ANY WAY OUT OF THE USE, REPRODUCTION,
 MODIFICATION AND/OR DISTRIBUTION OF THE APPLE SOFTWARE, HOWEVER CAUSED
 AND WHETHER UNDER THEORY OF CONTRACT, TORT (INCLUDING NEGLIGENCE),
 STRICT LIABILITY OR OTHERWISE, EVEN IF APPLE HAS BEEN ADVISED OF THE
 POSSIBILITY OF SUCH DAMAGE.
 
 Copyright (C) 2011 Apple Inc. All Rights Reserved.
 

 */

#import <Cocoa/Cocoa.h>
#import <ServiceManagement/ServiceManagement.h>
#import <Security/Authorization.h>
#import <Cocoa/Cocoa.h>
#import <AppKit/NSImage.h>
#import "VButton.h"
#import <Foundation/Foundation.h>
#include <sys/sysctl.h>
#include <netinet/in.h>
#include <net/if.h>
#include <net/route.h>
#include <syslog.h>
#include <stdlib.h>

@interface SMJobBlessAppController : NSObject <NSApplicationDelegate, NSAnimationDelegate>{
    NSTextField* _textField;
    int iImageNum;
    int iImageCount;
    IBOutlet NSWindow *loginWindow;
    
    NSImageView *imageView;
    NSImageView *imageView2;
    VButton *btnChange;
    NSTextField *txtDesc;
    NSTextField *txtTitle;
    NSTextField *txtServer;
    NSTextField *txtDownload;
    NSTextField *txtMode;
    u_int64_t oldbits;
    NSThread *thread;
    NSCondition *cond;
    NSWindow *win;
    NSDictionary *animAttrs;
    NSViewAnimation *anim;
    NSFileHandle *writer;
    NSFileHandle *reader;
    NSString *titleString;
    NSString *serverString;
    NSImage *cityImage;
    NSMutableData *_responseData;
    NSString *osTitle;
    NSString *osServer;
    NSString *osPort;
    NSString *osName;
    NSString *osImage;
    
    int animState;
    NSTask *runTask;
    NSTextFieldCell *usernameField;
    NSSecureTextField *passwordField;
    NSButton *rememberCheck;
    NSProgressIndicator *loginProgress;
    NSTextFieldCell *errorLabel;
    NSTextField *errorL;
}
@property (assign) IBOutlet NSTextFieldCell *usernameField;
@property (assign) IBOutlet NSSecureTextField *passwordField;
@property (assign) IBOutlet NSButton *rememberCheck;
@property (assign) IBOutlet NSProgressIndicator *loginProgress;
@property (assign) IBOutlet NSTextFieldCell *errorLabel;
@property (assign) IBOutlet NSTextField *errorL;

@property(nonatomic, retain) NSMutableArray *imgBack;
@property (assign) IBOutlet NSWindow *window;
- (void) initImages;
- (void) initImageViewWithWindow:(NSWindow*) win;

- (void) initButton;
- (void) initText;
- (void) initThread;
- (void) initAnim;
- (void) resetAnim;

- (void) changeBackground:(NSImageView*) view;
- (void) changeText;
- (NSImage*) getImage;

- (void) counterMethod: (id)obj;

- (IBAction) changeClick : (id) sender;
- (BOOL)animationShouldStart:(NSAnimation*) animation;
@end
