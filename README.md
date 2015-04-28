# PageXmlImageExtractor
Tool for extracting glyph images from a page xml file for a ocr image.

#Usage
ImageExtractor -i inputImageFile -x pageXml -o outputFolder


-i: Path to the ocr image from which the glyphs will be extracted.

-x: Path to the aletheia page xml file.

-o: Path to the folder to put all glyph images in.

#Description
Aletheia is a tool for creating ground thruth for a ocr image. It helps users to correct ocr scans for a given image so the user can define boxes for each scanned character and its correct Unicode character. These boxes and characters are saved in a so called page.xml. 

Franken+ is a tool which parsed this page.xml to create a new font for training tesseract. This font consists of the glyph defined by the bounding boxes in the page.xml. 

#Problem
Franken+ uses a external tool for extracting the actual glyph images from the ocr image. This tool only uses the bounding boxes to extract the glyph although Aletheia allows to define bounding polygons for glyph. For most fonts and characters the bounding boxes is sufficent, but for some characters it creates glyphs which contains parts from other glyphs. For example, the long s is a character whichs bounding box contains a part of the next character:

![alt tag](https://github.com/SvenLauterbach/GitHubAssets/blob/master/ImageExtractor/boundingPolygon.PNG)

Aletheia addresses this problem by providing the user with a tool which lets the user create a bounding polygon (the green line around the long s in the picture is such a bounding polygon where as the q has a normal bounding box).

#Solution
This image extractor uses the polygon to extract the glyph and therefor creates a clean glyph:

![alt tag](https://github.com/SvenLauterbach/GitHubAssets/blob/master/ImageExtractor/result.PNG?raw=true)

