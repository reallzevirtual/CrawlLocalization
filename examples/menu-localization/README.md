# Crawl Localization — __LANGUAGE__ menu demo

A minimal __NAME__ example created from the __VERSION__ language authoring kit.
Only six labels are translated: Start Game, The Vault, Options, Credits,
Quit and Back. Other text remains original English. This is not a full translation.

Requires Crawl for Windows and BepInEx 5 (32-bit).
With the game closed, extract this package into:
Crawl/BepInEx/plugins/CrawlLocalization/

Set the following in the existing BepInEx/config/crawl.localization.cfg,
preserving other settings:

```ini
[General]
Language = __LANGUAGE__
```

Restart after installing the font bundle. Both language folders can coexist;
change Language to ru or es and press F8 to switch tables. Language = en restores
original English. No Unity installation or system font installation is needed.
Keep all three files in Fonts/ (bundle, OFL and notice).

The bundle and tables were checked offline, including rendering in Unity 5.4.2.
Please verify menu positioning, accented letters and mouse selection in-game.
