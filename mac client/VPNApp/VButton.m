//
//  VButton.m
//  LotusALPHA
//
//  Created by Christofer Persson on 5/8/14.
//  Copyright (c) 2014 softk. All rights reserved.
//

#import "VButton.h"

@implementation VButton

int state = 0;

- (id)initWithFrame:(NSRect)frameRect  {
    self = [super initWithFrame:frameRect];
    if(self != nil) {
        NSLog(@"btn init");
    }
    return self;
}


- (void)mouseEntered:(NSEvent *)theEvent{
    NSLog(@"mouseEntered");
    [self setImage:nil];
    [self setNeedsDisplay];
}
- (void)mouseExited:(NSEvent *)theEvent{
    if(state == 0){
        [self setImage:[NSImage imageNamed:@"red3.png"]];
    }
    else{
        [self setImage:[NSImage imageNamed:@"green3.png"]];
    }
    [self setNeedsDisplay];
}

- (void)mouseDown:(NSEvent *)ev {
    if(state == 0){
        [self setImage:[NSImage imageNamed:@"green3.png"]];
        state = 1;
    }
    else{
        [self setImage:[NSImage imageNamed:@"red3.png"]];
        state = 0;
    }
    [self setNeedsDisplay];
    [self performClick: self];    
}

- (void)createTrackingArea
{
    NSTrackingAreaOptions focusTrackingAreaOptions = NSTrackingActiveInActiveApp;
    focusTrackingAreaOptions |= NSTrackingMouseEnteredAndExited;
    focusTrackingAreaOptions |= NSTrackingAssumeInside;
    focusTrackingAreaOptions |= NSTrackingInVisibleRect;
    
    NSTrackingArea *focusTrackingArea = [[NSTrackingArea alloc] initWithRect:NSZeroRect
                                                                     options:focusTrackingAreaOptions owner:self userInfo:nil];
    [self addTrackingArea:focusTrackingArea];
}


- (void)awakeFromNib
{
    [self createTrackingArea];
}
@end
