Real-Debrid torrent downloader
==========

The Goal of this project is to automaticlly download torrent from a (watched) directory and send it to a server to proxyfy the torrent in a regular HTTP one and send the download command to a download manager.

In this first version the torrent proxyfier is [RealDebrid](https://real-debrid.com) and the download manager is [Aria2C](https://aria2.github.io/manual/en/html/aria2c.html#synopsis) (I choose this one because of its daemon mode and RPC interface).


In the future I would migrate the solution to the mediator pattern architecture to expand the possibilites smoothlessly (like a progress tracker for instance) and provide more provider for the torrent proxyfier and to be compatible with more download manager.

[![Build Status](https://olihou.visualstudio.com/TorrentDownloader/_apis/build/status/olihou.TorrentDownloader?branchName=master)](https://olihou.visualstudio.com/TorrentDownloader/_build/latest?definitionId=1&branchName=master)
