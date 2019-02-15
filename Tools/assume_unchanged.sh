#!/bin/bash

files=`git diff --name-only | grep -E '.meta$' `
for file in $files; do
  git update-index --assume-unchanged $file
done
