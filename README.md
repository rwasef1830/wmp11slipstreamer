# wmp11slipstreamer
A tool to integrate Windows Media Player 11 setup into your Windows XP setup source. 

* Credits to boooggy from RyanVM Forums (https://ryanvm.net/forum/) for doing the hard work of writing the INF files 
that actually told setup how to install the Windows Media Player 11 files. This application is just a mini-integrator
that pulls the files from the original Microsoft setup packages and applies them to the setup source according to the rules
that he worked hard to write. Cheers my old friend :-)

* This code was previously split into 2 projects, the n7Framework library and the slipstreamer itself. The slipstreamer referenced a certain commit of n7Framework using the subversion equivalent of git subrepos and most of the commit history was in the n7Framework project.

* Unfortunately due to a hard disk failure, the commit history of the n7Framework project was lost. I managed to save only the working copy and merged it into the source tree of the slipstreamer thus enabling it to build.

* The project started out being hosted on a local subversion server, then got migrated to bitbucket mercurial, and now finally moved to GitHub due to bitbucket shutting down mercurial repos.
