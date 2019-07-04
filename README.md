# AnyPackageManagerCache (apmc)

Use apmc as your local registry server to speed up install packages.

Supports:

* pip (pypi)
* npm (npmjs)

## Usage

To use apmc, you need to run server in backgroud, then:

### Pip

Enable proxy for **pip**, edit `pip.ini` like:

``` ini
[global]
index-url = http://127.0.0.1:5000/pypi/simple/
```

`pip.ini` should locate at:

* windows: `%userprofile%\pip\pip.ini`
* unix: `~/.pip/pip.conf`

### Npm

Enable proxy for **pip**, set config in `.npmrc` like:

``` ini
registry = http://127.0.0.1:5000/npmjs/registry/
```
