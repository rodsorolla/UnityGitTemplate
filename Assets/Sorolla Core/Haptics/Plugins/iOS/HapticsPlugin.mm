#import <UIKit/UIKit.h>

extern "C" {

    void _HapticsPlayImpact(int intensity) {
        UIImpactFeedbackStyle style;
        switch (intensity) {
            case 0: style = UIImpactFeedbackStyleLight; break;
            case 1: style = UIImpactFeedbackStyleMedium; break;
            case 2: style = UIImpactFeedbackStyleHeavy; break;
            default: style = UIImpactFeedbackStyleLight; break;
        }
        UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:style];
        [generator prepare];
        [generator impactOccurred];
    }

    void _HapticsPlaySelection() {
        UISelectionFeedbackGenerator *generator = [[UISelectionFeedbackGenerator alloc] init];
        [generator prepare];
        [generator selectionChanged];
    }

    void _HapticsPlayNotification(int type) {
        UINotificationFeedbackType feedbackType;
        switch (type) {
            case 0: feedbackType = UINotificationFeedbackTypeSuccess; break;
            case 1: feedbackType = UINotificationFeedbackTypeWarning; break;
            case 2: feedbackType = UINotificationFeedbackTypeError; break;
            default: feedbackType = UINotificationFeedbackTypeSuccess; break;
        }
        UINotificationFeedbackGenerator *generator = [[UINotificationFeedbackGenerator alloc] init];
        [generator prepare];
        [generator notificationOccurred:feedbackType];
    }
}
