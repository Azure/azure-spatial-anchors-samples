// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
#import "BaseViewController.h"

@interface SharedDemoViewController : BaseViewController <UITextFieldDelegate>
{
    UIButton *_secondaryButton;
    UITextField *_textEntryControl;
    UITextField *_lastTextField;
}
@end
