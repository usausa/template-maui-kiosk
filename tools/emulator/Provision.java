import android.content.ComponentName;
import android.content.Intent;
import android.os.Bundle;
import android.os.IBinder;
import android.os.PersistableBundle;

import java.lang.reflect.Method;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.Iterator;

import org.json.JSONObject;

public final class Provision {
    private static final String COMPONENT_NAME = "android.app.extra.PROVISIONING_DEVICE_ADMIN_COMPONENT_NAME";

    public static void main(String[] args) throws Exception {
        JSONObject json = new JSONObject(new String(Files.readAllBytes(Paths.get(args[0])), StandardCharsets.UTF_8));

        Intent intent = new Intent("android.app.action.PROVISION_MANAGED_DEVICE_FROM_TRUSTED_SOURCE");
        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        intent.putExtra("android.app.extra.PROVISIONING_TRIGGER", 2);

        Iterator<String> keys = json.keys();
        while (keys.hasNext()) {
            String key = keys.next();
            Object value = json.get(key);
            if (COMPONENT_NAME.equals(key)) {
                intent.putExtra(key, ComponentName.unflattenFromString((String) value));
            } else if (value instanceof JSONObject) {
                JSONObject nested = (JSONObject) value;
                PersistableBundle bundle = new PersistableBundle();
                Iterator<String> names = nested.keys();
                while (names.hasNext()) {
                    String name = names.next();
                    bundle.putString(name, nested.getString(name));
                }
                intent.putExtra(key, bundle);
            } else if (value instanceof Boolean) {
                intent.putExtra(key, (Boolean) value);
            } else if (value instanceof Integer) {
                intent.putExtra(key, (Integer) value);
            } else {
                intent.putExtra(key, value.toString());
            }
        }

        System.out.println("intent=" + intent + " extras=" + intent.getExtras());
        System.out.println("result=" + Activities.start(intent));
    }
}
