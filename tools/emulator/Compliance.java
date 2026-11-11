import android.content.ComponentName;
import android.content.Intent;
import android.os.PersistableBundle;

public final class Compliance {
    public static void main(String[] args) throws Exception {
        Intent intent = new Intent("android.app.action.ADMIN_POLICY_COMPLIANCE");
        intent.setComponent(ComponentName.unflattenFromString(args[0]));
        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);

        PersistableBundle extras = new PersistableBundle();
        for (int i = 1; i < args.length; i++) {
            int separator = args[i].indexOf('=');
            extras.putString(args[i].substring(0, separator), args[i].substring(separator + 1));
        }
        intent.putExtra("android.app.extra.PROVISIONING_ADMIN_EXTRAS_BUNDLE", extras);

        System.out.println("intent=" + intent + " extras=" + extras);
        System.out.println("result=" + Activities.start(intent));
    }
}
