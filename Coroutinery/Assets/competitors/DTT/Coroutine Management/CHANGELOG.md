# Changelog

All notable changes to this package will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this package adheres to [Semantic Versioning](https://semver.org/)

## [7.1.1] 2022-05-17
### Updated
- Updated dependencies.
- Updated Unity version.

## [7.1.0] 2022-01-25
### Updated
- Updated dependencies to editor utilities, publishing tools and runtime utilities.

## [7.0.1] 2022-01-13
### Updated
- Updated dependencies to editor utilities, publishing tools and runtime utilities.

## [7.0.0] 2021-12-30
## Added
- Added examples

## Removed
- Removed Start method from public api for CoroutineWrapperBase

## [6.0.1] 2021-12-27
## Fixed
- Add missing version defines causing compile errors on user base with VSCode IDE.

## [6.0.0] 2021-12-14
## Updated
- Updated editor utilities dependency to 3.0.0

## [5.1.0] 2021-12-10
### Added
 - Added a WaitForFrames method to wait for a given amount of frames
 - Added a WaitForAll method to wait for multiple conditions/routines to finish
 - Added a WaitForAny method to wait for any of a collection of conditions/routines to finish

## [5.0.0] 2021-12-09
### Added
 - Added KickOffCoroutine() overload to only have ICoroutine as parameter.
 - Added internal CoroutineManger.KickOffCoroutine(). 
 - Added Unit-tests for CoroutineManger.StartUserCoroutine().

### Changed
 - CoroutineManger.StartCoroutine() is now used to track coroutines started on the CoroutineManager by the user.
 - CoroutineManger.StopCoroutine() is now internal CoroutineManger.StopCoroutine().
 - CustomeCoroutineWrapper now takes an IEnumerator in its constructor.

## [4.0.0] 2021-11-30
### Added
 - Added CoroutineWorker taking over the old functionality of CoroutineManager.
 - Added Unit-tests for CoroutineManager.WaitForCustomeYield()

### Changed
 - CoroutineManager has been changed into a static class instead of a singleton.
 Through the CoroutineManager the functionality of the CoroutineWorker will be accessable.

### Removed
 - Removed singleton core dependency.
 - Removed the need to use CoroutineManager.Instance.
 Instead of CoroutineManager.Instance.WaitUntilSceneIsLoaded() use CoroutineManager.WaitUntilSceneIsLoaded(), etc.

## [2.0.1] 2021-05-03
### Fixed
 - Problem with missing reference errors when imported in packages with a singleton core dependency of version 2.1.0 .

### Changed
 - Updated singleton core dependency to version 2.1.0

## [2.0.0] 2021-02-23
### Added
 - Update API with new Coroutines to use.
    - Wait until with a timeout feature.
    - Wait for a user defined CustomeYieldInstruction.
 - Added a new CoroutineWrapper type, that wraps around a Coroutine and has access to a larger interface
 and more data about the coroutine.
 - It's now possible to stop coroutines from executing.
 - Better exception throwing and handling.
 - Unit-tests for the CoroutineManager.

### Changed
 - CoroutineManager.Instance.WaitForSeconds() no longer returns a oroutine type, instead it returns a CoroutineWrapper.
 - The other methods didn't return anything before, but now also either return CoroutineWrapper or CustomCoroutineWrapper.

### Removed
 - CoroutineManager.Instance.WaitUntilSceneIsLoaded and CoroutineManager.Instance.WaitUntilSceneIsUnloaded are removed
 duo to them not having a huge use case. A better alternative would be SceneManager.SceneLoaded event.


## [1.0.0] - 2020-11-12
### Added
 - Base of the CoroutineManager as ussed in other projects.