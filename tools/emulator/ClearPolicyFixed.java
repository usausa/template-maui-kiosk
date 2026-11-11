import android.os.IBinder;

import java.lang.reflect.Method;

public final class ClearPolicyFixed {
    private static final int FLAG_PERMISSION_POLICY_FIXED = 1 << 2;

    public static void main(String[] args) throws Exception {
        Class<?> serviceManager = Class.forName("android.os.ServiceManager");
        IBinder binder = (IBinder) serviceManager.getMethod("getService", String.class).invoke(null, "permissionmgr");
        Object manager = Class.forName("android.permission.IPermissionManager$Stub").getMethod("asInterface", IBinder.class).invoke(null, binder);
        for (Method method : manager.getClass().getMethods()) {
            if (!method.getName().equals("updatePermissionFlags")) {
                continue;
            }

            Class<?>[] types = method.getParameterTypes();
            Object[] values = new Object[types.length];
            values[0] = args[0];
            values[1] = args[1];
            values[2] = FLAG_PERMISSION_POLICY_FIXED;
            values[3] = 0;
            values[4] = true;
            if (types.length == 7) {
                values[5] = types[5] == String.class ? "default:0" : (Object) 0;
            }
            values[types.length - 1] = 0;
            method.invoke(manager, values);
            System.out.println("cleared " + args[1]);
        }
    }
}
