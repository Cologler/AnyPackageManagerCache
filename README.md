# AnyPackageManagerCache (apmc)

Supports:

* pip (pypi)

## Usage

To use apmc, you need to run server in backgroud, then:

### Pip

Enable proxy for **pip**, edit `pip.ini` like:

``` ini
[global]
index-url = http://localhost:5000/pypi/simple/
```

`pip.ini` should locate at:

* windows: `%userprofile%\pip\pip.ini`
* unix: `~/.pip/pip.conf`
