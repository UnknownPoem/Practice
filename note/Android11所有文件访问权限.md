在AndroidManifest.xml中添加

```
<uses-permission android:name="android.permission.MANAGE_EXTERNAL_STORAGE" tools:ignore="ScopedStorage" />
```

这样做只是写明了需要这个权限，到手机上得手动去开。

自己测试打包apk时可以这样加，如果想要发布产品，应该在启动应用时，主动申请权限，并提供跳转到权限申请页面的东西。