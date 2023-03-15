# jpegdump
Simple C# console application that dumps the marker segments of a JPEG and JPEG-LS file.

Modifications by Patty-OFurniture to decode the FBMD metadata embedded in IPTC resources.  It's a series of hex characters embedded in JPEG files from FaceBook. It's a set of increasing values, and articles like this one have hyped it as some super privacy invading junk. Well, it's just** the offsets of all of the SOS JPEG markers**. JpegDump or ExifTool, should dump the markers to your satisfaction.

The image shows output from a modified JpegDump (Second link, C# version, with info from the first link, straight and confusing C). It dumps the FBMD medadata in DWORD chunks, and I have the results in Notepad++ with highlighting showing the matching offsets. Yes I had to stitch it together a bit to get multiple highlights.

FBMD 23 (I don't know what 23 means), 9 markers, and then each offset to a SOS marker. Not a privacy issue. There's another bit I'm looking at, but I don't have anything to share there. Point being, this particular tag seems harmless. I'd appreciate feedback, this is fairly preliminary but it matches every file I've tried.

![FBMD Example](https://github.com/Patty-OFurniture/jpegdump/raw/main/FBMD%20example.png)

After a few quick tests, it seems FaceBook just converts all progressive scans to baseline, making the FBMD marker irrelevant, and it no longer appears.
