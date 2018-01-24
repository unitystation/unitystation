#!/bin/bash
 
# ==> MODIFY THIS
 
#What user you want to use (default: anonymous)
STEAM_USER=anonymous
 
#If you are not using anonymous, specify a password here.
STEAM_PASS=
 
#The default location of the server, relative to this script (default: server).
#If no directory is specified for the server, it'll fall back on this one.
#Don't add a trailing /
INSTALL_DIR=server
 
#The location of the SteamCMD, relative to this script (default: bin). Don't add a trailing /
STEAM_DIR=bin
 
#Ids of the servers you want to install, leave empty to skip
#First item is the directory, second item is the AppID. Directory is relative to script directory
DL_DIR0=Release
DL_SV0=792890
 
DL_DIR1=
DL_SV1=
 
DL_DIR2=
DL_SV2=
 
DL_DIR3=
DL_SV3=
 
DL_DIR4=
DL_SV4=
 
DL_DIR5=
DL_SV5=
 
DL_DIR6=
DL_SV6=
 
DL_DIR7=
DL_SV7=
 
#Repeat this and the call to add_game at the bottom of this
#script to add more servers
 
# ==> (optional) INTERNAL SETTINGS, MODIFY IF REQUIRED
 
STEAMCMD_URL="http://media.steampowered.com/client/steamcmd_linux.tar.gz"
STEAMCMD_TARBALL="steamcmd_linux.tar.gz"
 
#
#	Don't modify below here, unless you know what you're doing.
#
 
#Get the current directory (snippet from SourceCMD's sourcecmd.sh)
BASE_DIR="$(cd "${0%/*}" && echo $PWD)"
 
#Relocate downloads to absolute url
INSTALL_DIR=$BASE_DIR/$INSTALL_DIR
STEAM_DIR=$BASE_DIR/$STEAM_DIR
 
if [ -z "$BASE_DIR" -o -z "$INSTALL_DIR" -o -z "$STEAM_DIR" ]; then
	echo "Base directory, Install directory or SteamCMD directory is empty."
	echo "Please check if lines 14 and 17 have content behind the = sign."
	exit 1
fi
 
if [ ! -e "$STEAM_DIR" ]; then
        mkdir $STEAM_DIR
        MADEDIR=$?
        if [ "$MADEDIR" != "0" ]; then
		echo "Failed to make directory for Steam. Do you have sufficient priviliges?"
		exit 1
	fi
	cd $STEAM_DIR
	echo "Fetching SteamCMD from servers..."
	wget $STEAMCMD_URL
	if [ ! -e "$STEAMCMD_TARBALL" ]; then
		echo "ERROR! Failed to get SteamCMD"
		exit 1
	fi
	echo "Completed. Extracting file..."
 
	#Hide the output
	(tar -xvzf $STEAMCMD_TARBALL)
 
	#Install SteamCMD now and try to login, if required
	if [ "$STEAM_USER" != "anonymous" ]; then
		$STEAM_DIR/steamcmd.sh +login $STEAM_USER $STEAM_PASS +quit
	else
		$STEAM_DIR/steamcmd.sh +quit
	fi
fi
 
cd $BASE_DIR
 
CmdArgs="+login $STEAM_USER $STEAM_PASS"
ShouldRun=0
 
add_game(){
	GAME="$1"
	DIR="$2"
	if [ ! -z "$GAME" ]; then
		if [ -z "$DIR" ]; then
			DIR=$INSTALL_DIR
		else
			DIR=$BASE_DIR/$DIR
		fi
 
		OK=0
		if [ ! -d "$DIR" ]; then
			echo "Creating directory $DIR..."
			(mkdir $DIR)
			if [ ! -d "$DIR" ]; then
				OK=1
			fi
		fi
		if [ "$OK" == "0" ]; then
			CmdArgs="$CmdArgs +force_install_dir \"$DIR\" +app_update $GAME validate"
			ShouldRun=1
		else
			echo "WARNING! Cannot add AppId $GAME into $DIR. Failed to create directory"
		fi
	fi
}
 
add_game "$DL_SV0" "$DL_DIR0"
add_game "$DL_SV1" "$DL_DIR1"
add_game "$DL_SV2" "$DL_DIR2"
add_game "$DL_SV3" "$DL_DIR3"
add_game "$DL_SV4" "$DL_DIR4"
add_game "$DL_SV5" "$DL_DIR5"
add_game "$DL_SV6" "$DL_DIR6"
add_game "$DL_SV7" "$DL_DIR7"
 
CmdArgs="$CmdArgs +quit"
 
if [ "$ShouldRun" == "0" ]; then
	echo "ERROR! No game IDs specified. Please specify at least one id"
	exit 1
fi
 
cd "$BASE_DIR"
 
#Workaround for SteamCMD continuously re-installing apps
echo "cd \"$BASE_DIR\"" > call.sh
echo "$STEAM_DIR/steamcmd.sh $CmdArgs" >> call.sh
chmod u+x ./call.sh
./call.sh
rm call.sh
 
echo "OK! Completed updating files!"
 
exit 0