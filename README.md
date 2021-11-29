# Git ci tool
An tool for ci base on git repository.  can generate version, changes notes etc.

[![Nuget](https://img.shields.io/nuget/v/Passingwind.Git-CI-Tools)](https://www.nuget.org/packages/Passingwind.Git-CI-Tools/)

``` shell
> gitci -h

Usage:
  gitci [options] [command]

Options:
  --project <project>   The project root path
  --branch <branch>     The project git branch. Default is current branch
  --include-prerelease  [default: False]
  --version             Show version information
  -?, -h, --help        Show help and usage information

Commands:
  version
  release
  git
```

``` shell
> gitci version -h
version

Usage:
  gitci [options] version [command]

Options:
  --project <project>   The project root path
  --branch <branch>     The project git branch. Default is current branch
  --include-prerelease  [default: False]
  -?, -h, --help        Show help and usage information

Commands:
  current  Show current version
  next     Generate next version
```

``` shell
> gitci release -h
release

Usage:
  gitci [options] release [command]

Options:
  --project <project>   The project root path
  --branch <branch>     The project git branch. Default is current branch
  --include-prerelease  [default: False]
  -?, -h, --help        Show help and usage information

Commands:
  changes  Generate changes from commit logs
```

``` shell 
> gitci git -h
git

Usage:
  gitci [options] git [command]

Options:
  --project <project>   The project root path
  --branch <branch>     The project git branch. Default is current branch
  --include-prerelease  [default: False]
  -?, -h, --help        Show help and usage information

Commands:
  changes
```
