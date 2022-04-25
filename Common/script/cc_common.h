//=============================================================================
//
// Adventure Game Studio (AGS)
//
// Copyright (C) 1999-2011 Chris Jones and 2011-20xx others
// The full list of copyright holders can be found in the Copyright.txt
// file, which is part of this source code distribution.
//
// The AGS source code is provided under the Artistic License 2.0.
// A copy of this license can be found in the file License.txt and at
// http://www.opensource.org/licenses/artistic-license-2.0.php
//
//=============================================================================
//
// Script options and error reporting.
//
//=============================================================================
#ifndef __CC_ERROR_H
#define __CC_ERROR_H

#include "util/string.h"

#define SCOPT_EXPORTALL      1   // export all functions automatically
#define SCOPT_SHOWWARNINGS   2   // printf warnings to console
#define SCOPT_LINENUMBERS    4   // include line numbers in compiled code
#define SCOPT_AUTOIMPORT     8   // when creating instance, export funcs to other scripts
#define SCOPT_DEBUGRUN    0x10   // write instructions as they are procssed to log file
#define SCOPT_NOIMPORTOVERRIDE 0x20 // do not allow an import to be re-declared
#define SCOPT_LEFTTORIGHT 0x40   // left-to-right operator precedance
#define SCOPT_OLDSTRINGS  0x80   // allow old-style strings
#define SCOPT_UTF8        0x100  // UTF-8 text mode

extern void ccSetOption(int, int);
extern int ccGetOption(int);

// error reporting

extern void cc_error(const char *, ...);

extern int ccError;             // set to non-zero if error occurs
extern int ccErrorLine;         // line number of the error
extern AGS::Common::String ccErrorString; // description of the error
extern AGS::Common::String ccErrorCallStack; // callstack where error happened
extern bool ccErrorIsUserError;
extern const char *ccCurScriptName; // name of currently compiling script

extern int currentline;

#endif // __CC_ERROR_H