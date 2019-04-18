// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#import "MenuScreenViewController.h"
#import "BasicDemoViewController.h"
#import "NearbyDemoViewController.h"
#import "SharedDemoViewController.h"

@implementation MenuScreenViewController

- (IBAction)sharedTap:(id)sender {
    NSLog(@"Starting Shared Demo");
    SharedDemoViewController * vc = [[SharedDemoViewController alloc] init];
    [self presentViewController:vc animated:NO completion:nil];
}

- (IBAction)nearbyTap:(id)sender {
    NSLog(@"Starting Nearby Demo");
    NearbyDemoViewController * vc = [[NearbyDemoViewController alloc] init];
    [self presentViewController:vc animated:NO completion:nil];
}

- (IBAction)basicTap:(id)sender {
    NSLog(@"Starting Basic Demo");
    BasicDemoViewController * vc = [[BasicDemoViewController alloc] init];
    [self presentViewController:vc animated:NO completion:nil];
}

- (void)viewDidLoad {
    [super viewDidLoad];
}

- (void)viewWillAppear:(BOOL)animated {
    [super viewWillAppear:animated];
}

- (void)viewWillDisappear:(BOOL)animated {
    [super viewWillDisappear:animated];
}

- (void)didReceiveMemoryWarning {
    [super didReceiveMemoryWarning];
    // Release any cached data, images, etc that aren't in use.
}

@end




