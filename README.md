# Cpceek

Tool for downloading Amstrad CPC game ROMs from QUOI DE NOUVEAU SUR FTP.NVG.NTNU.NO thanks to Nicolas Campbell for make his server freely available.

What does this tool do?
1. Downloads 'whatsnew.txt'. This file has the latest news about updates on the FTP server.
2. Downloads '00_index_full.txt' (contains detailed ROM info and more) and generates a list of ROMs to download.
3. Only downloads ROMs you don't have or where the file size differs. 
4. Generates a XML game info list based on the info found in the CPC index file.
5. Generates a Hyperspin compliant XML list for all ROMs you have downloaded.

## Usage

1. Set the 'RomsPath' in the Cpceek.exe.config file.
2. Run Cpceek.exe

NOTE: You can close the tool at anytime without the worry of a incomplete download etc as the tool checks if the file has fully downloaded and will download again if need be.


