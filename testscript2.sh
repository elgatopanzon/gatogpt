#!/usr/bin/env sh

######################################################################
# @author      : ElGatoPanzon (contact@elgatopanzon.io)
# @file        : testscript2
# @created     : Wednesday Nov 15, 2023 19:22:07 CST
#
# @description : Test script 2
######################################################################

echo "this script is being executed in another process"
echo "hopefully it works and the parent will wait for us"

TESTVAR="was set in another script"

echo "do you remember that $VARNAME?"
