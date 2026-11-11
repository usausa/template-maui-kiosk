import android.content.Intent;
import android.os.Bundle;
import android.os.IBinder;

import java.lang.reflect.Method;

final class Activities {
    private Activities() {
    }

    static Object start(Intent intent) throws Exception {
        Class<?> serviceManager = Class.forName("android.os.ServiceManager");
        IBinder binder = (IBinder) serviceManager.getMethod("getService", String.class).invoke(null, "activity");
        Object manager = Class.forName("android.app.IActivityManager$Stub").getMethod("asInterface", IBinder.class).invoke(null, binder);
        Method method = manager.getClass().getMethod(
            "startActivityAsUserWithFeature",
            Class.forName("android.app.IApplicationThread"),
            String.class,
            String.class,
            Intent.class,
            String.class,
            IBinder.class,
            String.class,
            int.class,
            int.class,
            Class.forName("android.app.ProfilerInfo"),
            Bundle.class,
            int.class);
        return method.invoke(manager, null, "com.android.shell", null, intent, null, null, null, 0, 0, null, null, 0);
    }
}
