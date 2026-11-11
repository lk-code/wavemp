# TagLib#

WaveMP reads the tags of audio and video files with TagLib# (NuGet package `TagLibSharp`, version 2.3.0, https://github.com/mono/taglib-sharp).

TagLib# is licensed under the GNU Lesser General Public License version 2.1 (LGPL-2.1-only), see `COPYING` in this directory. WaveMP uses it unchanged as a separate assembly (`TagLibSharp.dll` in `/app`), loaded at runtime (dynamic linking). The assembly can be replaced by another build of the same version, for example one built from the source code above.

The source code of TagLib# is available from https://github.com/mono/taglib-sharp (tag `TaglibSharp-2.3.0.0`) and from the NuGet package.
