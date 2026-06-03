// TowerGuard Phase 5 — iOS haptics native plugin.
// Surfaces a single C entry point (_TriggerHaptic) that maps an int code to one of
// Apple's UIFeedbackGenerator styles. Called from C# via [DllImport("__Internal")].
//
//   0 — light impact          1 — medium impact
//   2 — heavy impact          3 — notification: success
//   4 — notification: warning

#import <UIKit/UIKit.h>

extern "C" {
    void _TriggerHaptic(int type) {
        if (@available(iOS 10.0, *)) {
            switch (type) {
                case 0: {
                    UIImpactFeedbackGenerator *g =
                        [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
                    [g prepare];
                    [g impactOccurred];
                    break;
                }
                case 1: {
                    UIImpactFeedbackGenerator *g =
                        [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
                    [g prepare];
                    [g impactOccurred];
                    break;
                }
                case 2: {
                    UIImpactFeedbackGenerator *g =
                        [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
                    [g prepare];
                    [g impactOccurred];
                    break;
                }
                case 3: {
                    UINotificationFeedbackGenerator *g = [UINotificationFeedbackGenerator new];
                    [g prepare];
                    [g notificationOccurred:UINotificationFeedbackTypeSuccess];
                    break;
                }
                case 4: {
                    UINotificationFeedbackGenerator *g = [UINotificationFeedbackGenerator new];
                    [g prepare];
                    [g notificationOccurred:UINotificationFeedbackTypeWarning];
                    break;
                }
                default: break;
            }
        }
    }
}
