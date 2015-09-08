# myManga

[![Join the chat at https://gitter.im/BakaBox/myManga](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/BakaBox/myManga?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
Consolidates information from multiple sites.

### 3rd Party DLLs
* HtmlAgilityPack
* Ionic.Zip
* Microsoft.Windows.Shell
* SmartThreadPool

## About
My name is James Parks and started this project out of a need to keep track of, organize, and download the numerous manga I read, currently about 80 different manga that update at different rates. After I was unable to find suitable software I began work on a manga reader that evolved into the current myManga. 

I have used other downloader/readers including ComicRack, MangaRipper, and DomDomSoft, none of which felt like a complete package. ComicRack was great for already downloaded zip, rar, cbz, cbr files but it could not download new chapters easily, it had a plugin system which seemed neanderthal-ish, and it used a single file for a database so cloud syncing was a pain. MangaRipper could download manga chapters but you would need a separate reader to view what you had downloaded, and the user would need to specify the actual url for the manga, no search :`(, it was nice though because it was open source. DomDomSoft had both a reader and a download, again separate programs, with ads that could be removed for a small fee and get a few extra features, and it was closed source. From these experiences I wanted an application that would download and read, keep track of where I left off in the manga immediately, auto download chapters as I read, be able to access multiple sites, and that would be open source so it could grow and mature faster.

So in 2011 I began this endeavor, actually started with an old MangaFox only reader in 2010, but let's not go there, if anyone would like its source leave a message in the Discussions and I'll post it. I wanted the core of the application done first so my first release was a proof of concept and, well let's just say I threw some controls in a jar and dumped them on the application, not pretty, but the application did work. To make it easier to update alongside the manga sites, cause they l-o-v-e to change their site layout from time to time, I decided to incorporate a key feature in myManga, a plugin system, allowing the individual site parsing code to update quickly and easily mostly separate from the main program and allow 3rd parties to create their own and make them easy to add new sites. To overcome the cloud sync issue, I decided to separate the data that is stored for each manga into a separate file that can update and be uploaded on its own.

## Features
* Search all sites at the same time
* Uses manga databases in conjunction to the manga sites
* Multi-threaded downloads
* Redesigned plugin interfaces
* **Manga Site Plugins.**
* **Database Site Plugins.**
* **Continuous Reading.**
* Offline Storage
* Image Compression
* **Laptop Screen Brightness Control**
* Basic Reader Zoom - *Top of Reader*

### Planned Features
* Bookmarks
* Archive Chapters
* Language Support
* ...


## Supported Sites - myManga Plugins
* Manga
 * [Batoto](http://bato.to/)
 * [MangaReader.net](http://www.mangareader.net/)
 * [MangaPanda.com](http://www.mangapanda.com/)
 * [MangaHere.com](http://www.mangahere.com/)
 * [MangaTraders.org](http://mangatraders.org/)
* Databases
 * [MangaHelpers](http://www.mangahelpers.com/manga/)
 * [Anime News Network](http://www.animenewsnetwork.com/)
 * [Baka-Updates](http://www.mangaupdates.com/)
