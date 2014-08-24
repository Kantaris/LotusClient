//
//  VButton.h
//  LotusALPHA
//
//  Created by Christofer Persson on 5/8/14.
//  Copyright (c) 2014 softk. All rights reserved.
//

#import <Cocoa/Cocoa.h>

@interface VButton : NSButton
- (void)mouseEntered:(NSEvent *)theEvent;
- (void)mouseExited:(NSEvent *)theEvent;
- (void)mouseDown:(NSEvent *)ev;
- (void)mouseUp:(NSEvent *)theEvent;
- (void)createTrackingArea;
- (void)awakeFromNib;
@end
