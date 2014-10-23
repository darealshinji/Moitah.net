There is no installation, just unzip to a new folder in the "Program Files"
folder.  The .NET Framework 2.0 (x86) is required.  If you get a "could not
load file or assembly" error message for FLACDotNet or WavPackDotNet, make sure
the Visual C++ 2005 SP1 runtime files (x86) are installed.

As an alternative to the Browse button, the input path may be set by dragging
a .cue file onto the input path text box.

"Output Style" naming compared to Exact Audio Copy:
  CUE Tools:         Exact Audio Copy:
    Single File        Single WAV File
    Gaps Appended      Multiple WAV Files With Gaps (Noncompliant)
    Gaps Prepended     Multiple WAV Files With Corrected Gaps
    Gaps Left Out      Multiple WAV Files With Leftout Gaps

Audio filename formatting variables (case sensitive).  The values are obtained
from the CUE sheet only (not from any tags within the input audio files):
  %D - Album artist
  %C - Album title
  %N - Track number
  %A - Track artist
  %T - Track title

A "special character" is anything other than a-z, A-Z, 0-9, space, or
underscore.

Custom output path formatting variables (case sensitive).  The remove special
characters and replace spaces audio filename settings also apply to %D and %C
here:
  %D - Album artist
  %C - Album title
  %F - Filename of the input CUE sheet without extension
  %0 - Full directory of the input CUE sheet
  %x (where x is an integer) - Part of the directory of the input CUE sheet.
       For example if the input CUE sheet is located in C:\Stuff\CUEs, %1 = C:,
       %2 = Stuff, %3 = CUEs.  Negative numbers start from the end of the
       directory, i.e. %-1 = CUEs, %-2 = Stuff, %-3 = E:.
  %x:y (where x/y are integers) - Same as above but specifies a range.

"Write offset" is the same as "Write samples offset" in Exact Audio Copy.  For
negative offsets, silence is added at the beginning and samples are removed
from the end.  For positive offsets, samples are removed from the beginning and
silence is added at the end.

The "Preserve HTOA" (hidden track one audio) setting will cause an extra file
to be created when outputting a gaps appended CUE sheet if an index 0 for track
1 exists.  If disabled, a PREGAP line will be written instead and the HTOA will
be discarded.

The settings file is located in the "%AppData%\CUE Tools" folder.  For example,
on Windows XP if your username is "John", the settings folder is likely
"C:\Documents and Settings\John\Application Data\CUE Tools".
