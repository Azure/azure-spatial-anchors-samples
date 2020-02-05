# com.unity.ui.runtime

[![](https://badges.cds.internal.unity3d.com/packages/com.unity.ui.runtime/build-badge.svg?branch=master)](https://badges.cds.internal.unity3d.com/packages/com.unity.ui.runtime/build-info?branch=master)

This package enables UIElements to be used at runtime. It provides a few Components that can be added to GameObjects in order to display a UIElements panel in the game view and to send in-game events to the panel.

Requires Unity 2019.3.

A preview package is available on Unity internal artifactory. To use it, add

```
"registry": "https://artifactory.prd.cds.internal.unity3d.com/artifactory/api/npm/upm-candidates"
```


to your project's `manifest.json`. Also, add

```
"com.unity.ui.runtime": "0.0.3-preview",
```

to the dependency list.
