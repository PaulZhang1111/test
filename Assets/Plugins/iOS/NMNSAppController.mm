#import "UnityAppController.h"
#include "PluginBase/AppDelegateListener.h"

@interface NMNSAppController : UnityAppController
@end

IMPL_APP_CONTROLLER_SUBCLASS (NMNSAppController)

@implementation NMNSAppController

extern void UnitySendMessage(const char *, const char *, const char *);
NSString *URLString = @"";
// UIApplicationOpenURLOptionsKey was added only in ios10 sdk, while we still support ios9 sdk
- (BOOL)application:(UIApplication*)app openURL:(NSURL*)url options:(NSDictionary<NSString*, id>*)options
{
    id sourceApplication = options[UIApplicationOpenURLOptionsSourceApplicationKey], annotation = options[UIApplicationOpenURLOptionsAnnotationKey];
    NSMutableDictionary<NSString*, id>* notifData = [NSMutableDictionary dictionaryWithCapacity: 3];
    if (url) notifData[@"url"] = url;
    if (sourceApplication) notifData[@"sourceApplication"] = sourceApplication;
    if (annotation) notifData[@"annotation"] = annotation;
    AppController_SendNotificationWithArg(kUnityOnOpenURL, notifData);
   
    URLString = [url absoluteString];
    UnitySendMessage( "MRUnitItem", "setIOSCalledParam", [URLString UTF8String] );
   
    return YES;
}

@end
