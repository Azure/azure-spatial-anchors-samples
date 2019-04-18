// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#import "SharedDemoViewController.h"

// Set this string to the URL created when publishing your Shared anchor service in the Sharing sample.
// The format should be: https://<app_name>.azurewebsites.net/api/anchors
NSString *const SharingAnchorsServiceUrl = @"";

// This demo creates an anchor and saves its identifier into a sharing service. It returns an anchor number for you to write down.
// We then type that the anchor number (or a past anchor number - from a different device!) to get the Cloud Spatial Anchor identifier and locate the anchor.
@implementation SharedDemoViewController

-(void)moveToNextStepAfterCreateCloudAnchor{
    [_feedbackControl setHidden:YES];
    [_button setTitle:@"Anchor being saved to sharing service... " forState:UIControlStateNormal];

    [self postAnchor:self->_targetId completionHandler:^(NSString *anchorNumber, NSError *error) {
        self->_ignoreTaps = NO;
        NSString *infoMessage;
        if (error) {
            infoMessage = [NSString stringWithFormat:@"Failed to find Anchor to look for - %@", error.localizedDescription];
        }
        else {
            infoMessage = [NSString stringWithFormat:@"Anchor number = %@! \n\nWe saved Cloud Anchor Identifier %@ into our sharing service successfully 😁 Its anchor number is %@. You can now enter that in the locate portion of this demo and we'll look for the Cloud Anchor we just saved.", anchorNumber, self->_targetId, anchorNumber];
        }
        dispatch_async(dispatch_get_main_queue(), ^{
            [self->_errorControl setHidden:NO];
            [self->_button setTitle:@"Tap to go to start of the Sharing Demo" forState:UIControlStateNormal];
            [self showLogMessage:infoMessage here:self->_errorControl];
        });
        NSLog(@"%@", infoMessage);
        self->_step = DemoStepPrepare;

        [self stopSession];
    }];
}

-(void)moveToNextStepAfterAnchorLocated{
    [_feedbackControl setHidden:YES];
    [_button setTitle:@"Anchor found! Tap to finish demo" forState:UIControlStateNormal];
    _step = DemoStepStopSession;
}

- (void)secondaryButtonTap:(UIButton *)sender {
    _step = DemoStepEnterAnchorNumber;
    [self buttonTap:nil];
}

- (void)buttonTap:(UIButton *)sender {
    if (_ignoreTaps) {
        return;
    }
    switch (_step) {
        case DemoStepPrepare:
            [_button setTitle:@"Tap to create Anchor and save it to the service" forState:UIControlStateNormal];
            [_secondaryButton setHidden:NO];
            _step = DemoStepCreateCloudAnchor;
            break;
        case DemoStepCreateCloudAnchor:
            _ignoreTaps = YES;
            _currentlySavingAnchor = YES;
            _saveCount = 0;
            [_secondaryButton setHidden:YES];
            
            [self startSession];
            
            // When you tap on the screen, touchesBegan will call createLocalAnchor and create a local ARAnchor
            // We will then put that anchor in the anchorVisuals dictionary with a key of "" and call CreateCloudAnchor when there is enough data for saving
            // CreateCloudAnchor will call moveToNextStepAfterCreateCloudAnchor when its async method returns
            [_button setTitle:@"Tap on the screen to create an Anchor ☝️" forState:UIControlStateNormal];
            break;
        case DemoStepEnterAnchorNumber:
            [_errorControl setTitle:@"Enter the anchor number in the bar above ☝️ When you press return, we will get the cloud anchor identifier for this anchor number from the service. You can get this anchor number from creating an anchor in this demo." forState:UIControlStateNormal];
            [_errorControl setHidden:NO];
            [_textEntryControl setHidden:NO];
            [_secondaryButton setHidden:YES];
            [_button setHidden:YES];
            break;
        case DemoStepLookForAnchor:
            _ignoreTaps = YES;
            [self stopSession];
            [self startSession];
            
            // We will get a call to locateAnchorsCompleted when locate operation completes, which will call moveToNextStepAfterAnchorLocated
            [self lookForAnchor];
            break;
        case DemoStepStopSession:
            [self stopSession];
            [self moveToMainMenu];
            return;
        default:
            assert(false);
            _step = 0;
            return;
    }
}

#pragma mark - network sharing
- (void)getCloudAnchorIdentifier {
    [_button setTitle:[NSString stringWithFormat:@"Getting cloud identifier for anchor number: %@", _textEntryControl.text] forState:UIControlStateNormal];
    [_button setHidden:NO];
    [_errorControl setHidden:YES];
    [_textEntryControl setHidden:YES];
    
    NSString *anchorNumber = [NSString stringWithFormat:@"%lu", (unsigned long)[_textEntryControl.text integerValue]];
    [self getAnchor:anchorNumber completionHandler:^(NSString *anchorId, NSError *error) {
        NSString *infoMessage;
        NSString *actionMessage;
        if (error) {
            infoMessage = [NSString stringWithFormat:@"We got an error: %@.", error.localizedDescription];
            actionMessage = @"Tap to enter a new anchor number";
            self->_step = DemoStepEnterAnchorNumber;
        } else {
            
            infoMessage = [NSString stringWithFormat:@"Succesfully got the anchor identifier from the sharing service! The sharing service anchor number was %@ and the Cloud Anchor identifier was %@", anchorNumber, anchorId];
            actionMessage = @"Tap to locate this Cloud Anchor";
            self->_targetId = anchorId;
            self->_step = DemoStepLookForAnchor;
        }
        NSLog(@"%@", infoMessage);
        dispatch_async(dispatch_get_main_queue(), ^{
            [self->_errorControl setHidden:NO];
            [self->_errorControl setTitle:infoMessage forState:UIControlStateNormal];
            [self->_button setTitle:actionMessage forState:UIControlStateNormal];
        });
    }];
}

- (NSError * _Nullable)checkResponse:(NSURLResponse *)response{
    NSHTTPURLResponse *httpResponse = (NSHTTPURLResponse*)response;
    NSError *error = nil;
    if (httpResponse.statusCode < 200 | httpResponse.statusCode >= 300) {
        NSString * errorMessage = [NSString stringWithFormat:@"%ld: %@", (long)httpResponse.statusCode, [NSHTTPURLResponse localizedStringForStatusCode:httpResponse.statusCode]];
        NSDictionary *userInfo = @{ NSLocalizedDescriptionKey: errorMessage };
        error = [NSError errorWithDomain:[[NSBundle mainBundle] bundleIdentifier] code:-58 userInfo:userInfo];
    }
    return error;
}

/**
 * Posts an anchor identifier to sharing service
 * @param anchorId The Cloud Anchor identifier to save into the sharing service
 * @param completionHandler For error handling, or to get the anchorNumber for the posted anchor. anchorNumber increments by one each time an anchor is posted to the service
 */
- (void)postAnchor:(NSString *)anchorId completionHandler:(void (^)(NSString * _Nullable anchorNumber, NSError * error))completionHandler {
    NSURLSessionConfiguration *configuration = [NSURLSessionConfiguration ephemeralSessionConfiguration];
    NSURLSession *session = [NSURLSession sessionWithConfiguration:configuration];
    NSURL *url = [NSURL URLWithString:SharingAnchorsServiceUrl];
    NSMutableURLRequest *request = [[NSMutableURLRequest alloc] initWithURL:url];
    [request setHTTPMethod:@"POST"];
    NSString *bodyString = anchorId;
    NSData *postData = [bodyString dataUsingEncoding:NSUTF8StringEncoding allowLossyConversion:NO];
    NSString *contentLengthString = [NSString stringWithFormat:@"%lu", (unsigned long)postData.length];
    [request addValue:contentLengthString forHTTPHeaderField:@"Content-Length"];
    [request setHTTPBody:postData];
    NSURLSessionDataTask *task = [session dataTaskWithRequest:request completionHandler:^(NSData * _Nullable data, NSURLResponse * _Nullable response, NSError * _Nullable error) {
        if (error) {
            completionHandler(nil, error);
            return;
        }
        
        NSError * responseError = [self checkResponse:response];
        if (responseError) {
            completionHandler(nil, responseError);
            return;
        }
    
        NSString *anchorNumber = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
        completionHandler(anchorNumber, nil);
    }];
    [task resume];
}

/**
 * Gets an anchor identifier from the sharing service
 * @param anchorNumber the anchorNumber that we got from a previous call to postAnchor
 * @param completionHandler For error handling, or to get the Cloud Anchor Identifier for given anchorNumber
 */
- (void)getAnchor:(NSString *)anchorNumber completionHandler:(void (^)(NSString * data, NSError *error))completionHandler {
    NSURLSessionConfiguration *configuration = [NSURLSessionConfiguration ephemeralSessionConfiguration];
    NSURLSession *session = [NSURLSession sessionWithConfiguration:configuration];
    NSString *urlString = [NSString stringWithFormat:@"%@/%@", SharingAnchorsServiceUrl, anchorNumber];
    NSURL *url = [NSURL URLWithString:urlString];
    NSURLSessionDataTask *task = [session dataTaskWithURL:url completionHandler:^(NSData * _Nullable data, NSURLResponse * _Nullable response, NSError * _Nullable error) {
        if (error) {
            completionHandler(nil, error);
            return;
        }
        
        NSError * responseError = [self checkResponse:response];
        if (responseError) {
            completionHandler(nil, responseError);
            return;
        }
        
        NSString *anchorId = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
        completionHandler(anchorId, nil);
    }];
    [task resume];
}

#pragma mark - UITextFieldDelegate

- (void)textFieldDidBeginEditing:(UITextField *)textField  {
    _lastTextField = textField;
}

- (void)textFieldDidEndEditing:(UITextField *)textField reason:(UITextFieldDidEndEditingReason)reason {
    [textField resignFirstResponder];
    _lastTextField = nil;
    if (reason != UITextFieldDidEndEditingReasonCommitted) {
        return;
    }
    if (textField != _textEntryControl) {
        return;
    }
    if (_textEntryControl.text.length == 0) {
        return;
    }

    [self getCloudAnchorIdentifier];
}

- (BOOL)textFieldShouldReturn:(UITextField *)textField {
    [self textFieldDidEndEditing:textField reason:UITextFieldDidEndEditingReasonCommitted];
    return YES; // NOT to ignore 'return'
}

#pragma mark - View Management

- (void)viewDidLoad {
    [super viewDidLoad];
    
    // Secondary button for use in sharing sample
    _secondaryButton = [self addButtonAt:super.sceneView.bounds.size.height - 140 lines:1];
    [_secondaryButton addTarget:self action:@selector(secondaryButtonTap:) forControlEvents:UIControlEventTouchDown];
    [_secondaryButton setTitle:@"Tap to locate Anchor by its anchor number" forState:UIControlStateNormal];
    
    // Control for text entry
    _textEntryControl = [UITextField new];
    float wideSize = super.sceneView.bounds.size.width - 20;
    _textEntryControl.keyboardType = UIKeyboardTypeNumbersAndPunctuation;
    _textEntryControl.returnKeyType = UIReturnKeySearch;
    _textEntryControl.frame = CGRectMake(10, 60, wideSize, 40);
    _textEntryControl.delegate = self;
    [_textEntryControl setBackgroundColor:[UIColor.lightGrayColor colorWithAlphaComponent:0.9]];
    _textEntryControl.placeholder = @"Anchor number";
    [self.view addSubview:_textEntryControl];
    [_textEntryControl setHidden:YES];
    
    if ([SharingAnchorsServiceUrl isEqual: @""])
    {
        [_secondaryButton setHidden:YES];
        [_button setHidden:YES];
        [_errorControl setHidden:NO];
        [self showLogMessage:@"Set SharingAnchorsServiceUrl in SharedDemoViewController.m" here:_errorControl];
        return;
    }
}

@end
