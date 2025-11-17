## ReOsuStoryboardPlayer.Avalonia
---------------
### Introduction
This repository is developed based on another repository [ReOsuStoryboardPlayer.Core](https://github.com/MikiraSora/ReOsuStoryboardPlayer/tree/master/ReOsuStoryboardPlayer.Core), primarily for learning Avalonia development and NativeAOT.
Program can build/run on Windows/Browser/Android

### Play & Download
[Browser Online](https://mikirasora.github.io/ReOsuStoryboardPlayer.Avalonia/) (Play [Demo1](https://mikirasora.github.io/ReOsuStoryboardPlayer.Avalonia?loadBeatmapSetId=548679) [Demo2](https://mikirasora.github.io/ReOsuStoryboardPlayer.Avalonia?loadBeatmapSetId=402680) [Demo3](https://mikirasora.github.io/ReOsuStoryboardPlayer.Avalonia?loadBeatmapSetId=499488))

Android: [arm64](https://mikirasora.github.io/ReOsuStoryboardPlayer.Avalonia/downloads/android_arm64.apk) | [x86_64](https://mikirasora.github.io/ReOsuStoryboardPlayer.Avalonia/downloads/android_x64.apk)

Windows: [x64](https://mikirasora.github.io/ReOsuStoryboardPlayer.Avalonia/downloads/win_x64.zip)

### Technology

* UI Framework: [Avalonia](https://github.com/AvaloniaUI/Avalonia)
* Rendering Implementation: [SkiaSharp](https://github.com/mono/SkiaSharp)
* Storyboard Implementation: [ReOsuStoryboardPlayer.Core](https://github.com/MikiraSora/ReOsuStoryboardPlayer/tree/master/ReOsuStoryboardPlayer.Core)
* Audio Implementation: [WASAPI&Media Foundation (Windows) Powered by DirectNAot](https://github.com/smourier/DirectNAot) / [WebAudio](https://developer.mozilla.org/zh-CN/docs/Web/API/Web_Audio_API)

### Todos:

- [x] Fix/Support FireFox
- [ ] ~~Support multithread in browser (migrate to .Net9/.Net10)~~
- [x] Fix/Optimize render
- [x] Integrate content
- [ ] Add Storyboard debugger who want debug his storyboard?
- [x] Support Android
- [x] Fix memory leak in heavy/cool storybaord
- [ ] Use low level render implement(Skia's performance and API design no longer meet requirements)
- [ ] Optimize others

### Screenshot
<img width="1915" height="1001" alt="image" src="https://github.com/user-attachments/assets/59d5471d-2586-4f1f-8441-49cab3e95ec1" />
<img width="1440" height="753" alt="image" src="https://github.com/user-attachments/assets/a75e5bec-b413-4ad6-87f8-f2489a438c58" />

