# --------------------------------------------------------------------------
# Gurux Ltd
#
# Copyright (c) Gurux Ltd.
#
# This file is part of Gurux Device Framework.
#
# Commit: $Format:%H$
# Date:   $Format:%cI$
#
# This software is available under either of the following licenses:
#
# 1. GNU General Public License, version 2 only (GPL-2.0-only).
#    See the LICENSE file included with this distribution.
#
# 2. A commercial license granted by Gurux Ltd under a separate written
#    agreement. Use under this option requires a valid commercial license
#    and is governed by the terms of that agreement.
#
# If you have not obtained a commercial license from Gurux Ltd,
# use of this software is governed by GPL-2.0-only.
#
# Commercial licensing: https://www.gurux.fi/
#
# SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
# --------------------------------------------------------------------------

"""Register this Linux build's existing icon and launcher for the current user."""
import os
from pathlib import Path
import shutil
import sys


def quote_executable(executable):
    """Quote an absolute executable path through both desktop-entry escaping layers."""
    value = str(executable)
    if any(character in value for character in "\r\n\t"):
        raise ValueError(value)
    value = value.replace("%", "%%").replace("\\", "\\\\")
    for character in ('"', "`", "$"):
        value = value.replace(character, "\\" + character)
    return ('"' + value + '"').replace("\\", "\\\\")


def install():
    """Install a launcher and matching hicolor icon into the user's XDG data home."""
    if sys.platform != "linux":
        raise OSError(sys.platform)
    directory = Path(__file__).resolve().parent
    executable = directory.parent / "Gurux.Serial.Example"
    icon = directory / "org.gurux.examples.serial.png"
    template = directory / "org.gurux.examples.serial.desktop"
    for source in (executable, icon, template):
        if not source.is_file():
            raise FileNotFoundError(source)
    entry = template.read_text(encoding="utf-8")
    # GIO checks the executable before expanding %% field escapes. A stable env
    # executable lets an application directory containing a literal % launch too.
    entry = entry.replace("Exec=Gurux.Serial.Example", "Exec=/usr/bin/env " + quote_executable(executable))
    configured_home = os.environ.get("XDG_DATA_HOME", "")
    data_home = Path(configured_home) if configured_home and Path(configured_home).is_absolute() else Path.home() / ".local" / "share"
    applications = data_home / "applications"
    icons = data_home / "icons" / "hicolor" / "1024x1024" / "apps"
    applications.mkdir(parents=True, exist_ok=True)
    icons.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(icon, icons / icon.name)
    destination = applications / template.name
    with destination.open("w", encoding="utf-8", newline="\r\n") as output:
        output.write(entry)
    return destination


if __name__ == "__main__":
    print(install())
