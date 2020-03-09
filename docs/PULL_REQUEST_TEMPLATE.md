# Pull Request Template

### Purpose
_Describe the problem the PR fixes or the feature it introduces_<br>
_Don't forget to use "Fixes #issuenumber" to select issues and auto close them on merge_

### Notes:
_Please enter any other relevant information here_

### Please make sure you have followed the self checks below before submitting a PR:

- [ ] Code is sufficiently commented
- [ ] Code is indented with tabs and not spaces
- [ ] The PR does not include any unnecessary .meta, .prefab or **.unity (scene) changes**
- [ ] The PR does not bring up any new compile errors
- [ ] The PR has been tested in editor
- [ ] Any new files are named using PascalCase (to avoid issues on case sensitive file systems)
- [ ] Any new / changed components follow the [Component Development Checklist](https://github.com/unitystation/unitystation/wiki/Component-Development-Checklist)
- [ ] Any new objects / items follow the [Creating Items and Objects Guide](https://github.com/unitystation/unitystation/wiki/Creating-Items-and-Objects%3A-Now-With-Prefab-Variants) (especially concerning the use of prefab variants)
- [ ] The PR has been tested in multiplayer (with 2 clients and late joiners, if applicable)
- [ ] The PR has been tested with round restarts.
- [ ] The PR has been tested on moving / rotating / rotated-before-joining matrices (if applicable)
