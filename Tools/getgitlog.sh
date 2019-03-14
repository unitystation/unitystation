#!/usr/bin/env bash

reldir=`dirname $0`
cd $reldir
cd ..
cd UnityProject
cd Assets
cd StreamingAssets
directory=`pwd`

echo "Directory is $directory"

git log -n 10\
    --pretty=format:'{"commit": "%H", "author": "%aN", "date": "%ai", "message": """%B""", "notes": """%N""" },' \
    $@  | awk 'BEGIN { print("[") } { print($0) } END { print("]") }' | python -u -c \
'import ast,json,sys; print(json.dumps(ast.literal_eval(sys.stdin.read())))' \
	> changelog.json