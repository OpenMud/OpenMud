# [2.10.0](https://github.com/OpenMud/OpenMud/compare/v2.9.0...v2.10.0) (2023-11-25)


### Features

* **compiler:** Added support for field initializers adjacent to new statements. I.e "var x = new {x = 20; y = 30} () ([0ff267a](https://github.com/OpenMud/OpenMud/commit/0ff267aee4128938455de6f26b1807072eee293d))
* **compiler:** Added support to omit arguments by leaving them blank: i.e "mycall(a,,c,d,,f)" ([585b64f](https://github.com/OpenMud/OpenMud/commit/585b64f371d409bff752a970811a5eda8f5bc2c2))
* **compiler:** Changed switch to support all kinds of expressions with an if-clause, not just integers. ([f42a344](https://github.com/OpenMud/OpenMud/commit/f42a3440f4d202ba28aea5c1878614cb2707a00a))

# [2.9.0](https://github.com/OpenMud/OpenMud/compare/v2.8.0...v2.9.0) (2023-11-24)


### Bug Fixes

* **compiler:** Added necessary support for "for loops" with useless expressions as initializers. For example "for(my_var, ..., ...)", in this case the initializer has no effect, but standard DreamMaker grammer will compile it anyways. ([36e5a22](https://github.com/OpenMud/OpenMud/commit/36e5a227aa7f64ab8b5d53b4c006092813e542e7))


### Features

* **compiler:** Added support for complex default argument expressions which require an execution context. For example "/proc/myproc(var/x = testglobal(14) + 13) { ... }" ([f13f195](https://github.com/OpenMud/OpenMud/commit/f13f19519dc560909cc26cca6f6fea829549b683))

# [2.8.0](https://github.com/OpenMud/OpenMud/compare/v2.7.0...v2.8.0) (2023-11-23)


### Bug Fixes

* **compiler:** Added test case for "as null" assertion & resolved associated issues ([fc03d91](https://github.com/OpenMud/OpenMud/commit/fc03d9109d271df4e13dc181e1111ccd26304eed))
* **compiler:** Changed casting semantics to type assertion semantics. The proper application of 'as' in an expression is as type assertion, not casting. ([fadfa40](https://github.com/OpenMud/OpenMud/commit/fadfa40561e53a2b45123aee072b1c1af5f22531))
* **compiler:** Object declarations are permitted to have a hanging forward slash (i.e '/mob/test/' in an object declaration is treated the same as '/mob/test') ([21cc5ca](https://github.com/OpenMud/OpenMud/commit/21cc5ca04ba690ac77854da64c511bbb80fc8b48))


### Features

* **compiler:** Added basic casing between primitives (with as keyword) support. ([6b5eadb](https://github.com/OpenMud/OpenMud/commit/6b5eadb5cb7782fd1ec9aa9e3963f8ad054a4564))
* **compiler:** Added compile support for setting proc "background" setting. ([f7c7df5](https://github.com/OpenMud/OpenMud/commit/f7c7df55456f9c4088f893c37be3291888b0cb6f))
* **compiler:** Added support for "instant" setting. ([d4cd650](https://github.com/OpenMud/OpenMud/commit/d4cd6506f39b8ac8fe31339397796dc2e7d89a32))
* **compiler:** Added support for break / continue keywords. ([ec8b9c5](https://github.com/OpenMud/OpenMud/commit/ec8b9c5df3a194ea3ce27e6c06fb23a5ed9fc589))
* **compiler:** Added support for scientific notation (i.e 2e10) numeric literals. ([3792656](https://github.com/OpenMud/OpenMud/commit/37926562f82c4e69d16d1a8db42ab3266c3d657f))
* **compiler:** Added support for the "hidden" setting. ([d360e96](https://github.com/OpenMud/OpenMud/commit/d360e9618ecf3ccd70c781e17945a4017d48002c))
* **compiler:** Added support for unary assignment operations (++ / --) on lists. I.e allowing for "myarray[2]++" or "++myarray[2]" expressions. Additionally, added associated test cases. ([59642be](https://github.com/OpenMud/OpenMud/commit/59642be7507b69103bcd38d3d6ef74bd19ca300d))
* **compiler:** Allow hanging forward slash in type name expressions (i.e "istype(x, /mob/example/)" ) ([58c1b4e](https://github.com/OpenMud/OpenMud/commit/58c1b4ee7255e442f0fced7fd66a8f130c1f5b24))
* **compiler:** Better istype support (now supports expressions such as "istype(<some expression>.field_name)", i.e where type hint resolution must occur at runtime. ) ([bda5fd4](https://github.com/OpenMud/OpenMud/commit/bda5fd43e65252e51d29511932ceebbf213fb129))

# [2.7.0](https://github.com/OpenMud/OpenMud/compare/v2.6.0...v2.7.0) (2023-11-13)


### Bug Fixes

* **compiler:** Better handling for variatons to asset file format. ([587026d](https://github.com/OpenMud/OpenMud/commit/587026d129c0412b48525149a8d2e9c1c1f2d3d2))
* **compiler:** Resolved issue with preprocessor transforming literal/constant strings into complex expressions that weren't supported by compiler in the specific context (namely, field initializer escape sequences were resulting in addtext expressions.) ([675d6da](https://github.com/OpenMud/OpenMud/commit/675d6dad6aba720942e4b769b5431f04df897e37))
* **compiler:** Support embedded (and unescaped) single/double quote character in multiline strings. ([d59a7f8](https://github.com/OpenMud/OpenMud/commit/d59a7f834086dcb9a2b285fe40d7348203027ab5))


### Features

* **compiler:** Optimized preprocessing, added support for complex string colacing, multiline string etc. ([4316e85](https://github.com/OpenMud/OpenMud/commit/4316e851976d3dadf030c6ce4c68818e7fbe3741))
* **compiler:** Proper handling of text macros. ([1db6f66](https://github.com/OpenMud/OpenMud/commit/1db6f66e6b53e6b50de59ca2d2efe3a0fdad7697))

# [2.6.0](https://github.com/OpenMud/OpenMud/compare/v2.5.1...v2.6.0) (2023-10-29)


### Features

* **compiler:** Optimized preprocessing. ([8921c77](https://github.com/OpenMud/OpenMud/commit/8921c77aface739c06c355045200c324650edc2e))

## [2.5.1](https://github.com/OpenMud/OpenMud/compare/v2.5.0...v2.5.1) (2023-10-23)


### Bug Fixes

* **compiler:** Fixed issue where default parameter values were not being respected. Added test cases for coverage. ([70dcf8a](https://github.com/OpenMud/OpenMud/commit/70dcf8a99cc922e0b0f039f249deff54531a4477))

# [2.5.0](https://github.com/OpenMud/OpenMud/compare/v2.4.0...v2.5.0) (2023-10-22)


### Bug Fixes

* **core:** AsText DmlEnv helper function now supports processing EnvObjectReference types. ([577545d](https://github.com/OpenMud/OpenMud/commit/577545d29c532bd742b033bfcfbae07c6495a068))
* **core:** Proper escape sequence in command strings. ([4a8e6d4](https://github.com/OpenMud/OpenMud/commit/4a8e6d40c70ad699d07e6cd82c40199c858838be))


### Features

* **compiler:** Added support for manipulating & evaluating the pre-return assignment ([f039031](https://github.com/OpenMud/OpenMud/commit/f03903181334375382694b0379214263087a10c3))
* **core:** Added string interpolation support & test cases. Added 'text' utility function implementation. ([50122f5](https://github.com/OpenMud/OpenMud/commit/50122f5cb83924c030378e881d2aacf510f8df5e))
* **networking:** Added ECS Message synchronization support. Added entity/world echo & sound configure message synchronization. ([33d97c0](https://github.com/OpenMud/OpenMud/commit/33d97c09cb73881f5516ab8fae7107a3d3a65707))

# [2.4.0](https://github.com/OpenMud/OpenMud/compare/v2.3.1...v2.4.0) (2023-10-18)


### Bug Fixes

* **core:** Fixed unhandled null reference. ([b2c25d2](https://github.com/OpenMud/OpenMud/commit/b2c25d202f7b2d45ed00da26566e32a42126c658))


### Features

* **core:** Added basic audio support to framework + ecs, also included some test cases. ([51bd8ae](https://github.com/OpenMud/OpenMud/commit/51bd8ae44befb4c9425f29f27d2977a4181ac554))

## [2.3.1](https://github.com/OpenMud/OpenMud/compare/v2.3.0...v2.3.1) (2023-10-18)


### Bug Fixes

* **CICD:** Fix typo in metadata ([0a713fb](https://github.com/OpenMud/OpenMud/commit/0a713fb02e166d01b3d843891d72149cf73b5b35))

# [2.3.0](https://github.com/OpenMud/OpenMud/compare/v2.2.1...v2.3.0) (2023-10-18)


### Features

* **core:** Add package metadata. ([262a35c](https://github.com/OpenMud/OpenMud/commit/262a35c4b2d4b69326fb8885d65540ae94d12bf4))

## [2.2.1](https://github.com/OpenMud/OpenMud/compare/v2.2.0...v2.2.1) (2023-10-17)


### Bug Fixes

* **core:** Resolved issue where vision was computed incorrectly, and performance was very poor. ([aadb46a](https://github.com/OpenMud/OpenMud/commit/aadb46af27c9eb76eb944a807d3f4b8f46f06712))

# [2.2.0](https://github.com/OpenMud/OpenMud/compare/v2.1.1...v2.2.0) (2023-10-16)


### Features

* **build:** Still trying to get this to work. ([215aa72](https://github.com/OpenMud/OpenMud/commit/215aa724e89e9501e0e52d70c7c31efd9e85885b))

## [2.1.1](https://github.com/OpenMud/OpenMud/compare/v2.1.0...v2.1.1) (2023-10-15)


### Bug Fixes

* **build:** Its a backslash not a forward slash... ([b48896a](https://github.com/OpenMud/OpenMud/commit/b48896abc9f5749c42a18ef06517f228f5032f35))
* **build:** Trying to fix the build. ([bb3622d](https://github.com/OpenMud/OpenMud/commit/bb3622dc8d99ced4fac1f0c4eed6acec51e7c104))
* **common:** Moved scaffold repo. ([5980138](https://github.com/OpenMud/OpenMud/commit/5980138c70fca989ef0d03c4de866bba74c76930))

## [2.1.1](https://github.com/OpenMud/OpenMud/compare/v2.1.0...v2.1.1) (2023-10-15)


### Bug Fixes

* **build:** Its a backslash not a forward slash... ([b48896a](https://github.com/OpenMud/OpenMud/commit/b48896abc9f5749c42a18ef06517f228f5032f35))
* **build:** Trying to fix the build. ([bb3622d](https://github.com/OpenMud/OpenMud/commit/bb3622dc8d99ced4fac1f0c4eed6acec51e7c104))

# [2.1.0](https://github.com/OpenMud/OpenMud/compare/v2.0.2...v2.1.0) (2023-10-15)


### Features

* **build:** Trying to get releases & docs to work. ([b6890d4](https://github.com/OpenMud/OpenMud/commit/b6890d42aef20c5c7c55a9012d296fcf31cb7d45))

## [2.0.2](https://github.com/JeremyWildsmith/OpenMud/compare/v2.0.1...v2.0.2) (2023-10-15)


### Bug Fixes

* **common:** Fixing build and testing release. ([8514d57](https://github.com/JeremyWildsmith/OpenMud/commit/8514d57237c6477737e1d1a6253ad899a095cf15))

## [2.0.1](https://github.com/JeremyWildsmith/OpenMud/compare/v2.0.0...v2.0.1) (2023-10-15)


### Bug Fixes

* **build:** Re-enable nuget releases. ([c1b757d](https://github.com/JeremyWildsmith/OpenMud/commit/c1b757de206217113ffdc798707849f96f12cb15))

# [2.0.0](https://github.com/JeremyWildsmith/OpenMud/compare/v1.0.0...v2.0.0) (2023-10-15)


### Bug Fixes

* **build:** Fix the build ([71ec779](https://github.com/JeremyWildsmith/OpenMud/commit/71ec7798af9564051c86498e238272f5490ca49e))


### BREAKING CHANGES

* **build:** Fix the build

# 1.0.0 (2023-10-15)


### Bug Fixes

* **build:** Force Breaking Change ([a4b87a7](https://github.com/JeremyWildsmith/OpenMud/commit/a4b87a7082bf48053786b33b23033a07b40913f1))


### Features

* **build:** Force advance major version. ([aae9402](https://github.com/JeremyWildsmith/OpenMud/commit/aae9402d265ff201a29e290586b34d2fa5b95ac2))
* **common:** Implemented CI/CD Pipelines. ([cfe4c3a](https://github.com/JeremyWildsmith/OpenMud/commit/cfe4c3ac49803b48a4ab2a788b26c1ed39ef16de))


### BREAKING CHANGES

* **build:** Force breaking change
* **build:** Force advance major version.

# 1.0.0 (2023-10-15)


### Features

* **build:** Force advance major version. ([aae9402](https://github.com/JeremyWildsmith/OpenMud/commit/aae9402d265ff201a29e290586b34d2fa5b95ac2))
* **common:** Implemented CI/CD Pipelines. ([cfe4c3a](https://github.com/JeremyWildsmith/OpenMud/commit/cfe4c3ac49803b48a4ab2a788b26c1ed39ef16de))


### BREAKING CHANGES

* **build:** Force advance major version.

# [1.4.0](https://github.com/JeremyWildsmith/OpenMud/compare/v1.3.0...v1.4.0) (2023-10-15)


### Features

* **build:** Trying to get CI/CD to work. ([cfc2215](https://github.com/JeremyWildsmith/OpenMud/commit/cfc22151af20f14fd7f0935f4adc8ab22fb160a6))
* **build:** Trying to get CI/CD to work. ([39bf06c](https://github.com/JeremyWildsmith/OpenMud/commit/39bf06c6c3da09977ed0bc0a734d6e0e41c5287c))
* **build:** Trying to get CI/CD to work. ([ffb4717](https://github.com/JeremyWildsmith/OpenMud/commit/ffb471750f910d3feddd1320c2aade97c7e1f0bc))
* **build:** Trying to get CI/CD to work. ([758d2fd](https://github.com/JeremyWildsmith/OpenMud/commit/758d2fdc15afe53da6634aad06d30b7c04148c72))
* **build:** Trying to get CI/CD to work. ([8cbf84d](https://github.com/JeremyWildsmith/OpenMud/commit/8cbf84d47e0f649c8e355dc429e41bcc9fff7a58))

# [1.3.0](https://github.com/JeremyWildsmith/OpenMud/compare/v1.2.0...v1.3.0) (2023-10-14)


### Features

* **build:** Trying to get CI/CD to work. ([acb891d](https://github.com/JeremyWildsmith/OpenMud/commit/acb891d33e1df4b8b269bf6141225233b10602d4))
* **build:** Trying to get CI/CD to work. ([03fe85c](https://github.com/JeremyWildsmith/OpenMud/commit/03fe85c4cac9c0fd61131c17dc1ab5b940629d4b))
* **build:** Trying to get CI/CD to work. ([0f6bdce](https://github.com/JeremyWildsmith/OpenMud/commit/0f6bdce432e7f1777804951a39d302836b775f07))
* **build:** Trying to get CI/CD to work. ([28d0f63](https://github.com/JeremyWildsmith/OpenMud/commit/28d0f63fcabd3ed25a5b9058f794961548b8fea1))

# [1.2.0](https://github.com/JeremyWildsmith/OpenMud/compare/v1.1.0...v1.2.0) (2023-10-14)


### Features

* **build:** Trying to get CI/CD to work. ([09a5012](https://github.com/JeremyWildsmith/OpenMud/commit/09a501223206ab47290ba38740a55aea2b433132))

# [1.1.0](https://github.com/JeremyWildsmith/OpenMud/compare/v1.0.3...v1.1.0) (2023-10-14)


### Features

* **build:** Trying to get CI/CD to work. ([fd0db18](https://github.com/JeremyWildsmith/OpenMud/commit/fd0db18a0716a14a4a05134063db72105c82a5da))

## [1.0.3](https://github.com/JeremyWildsmith/OpenMud/compare/v1.0.2...v1.0.3) (2023-10-14)


### Bug Fixes

* **build:** Trying to get CI/CD to work. ([2ac2f86](https://github.com/JeremyWildsmith/OpenMud/commit/2ac2f86922d260c21b63cba503bdfabb09e70b4f))

## [1.0.2](https://github.com/JeremyWildsmith/OpenMud/compare/v1.0.1...v1.0.2) (2023-10-14)


### Bug Fixes

* **build:** Trying to get CI/CD to work. ([c6ef5d9](https://github.com/JeremyWildsmith/OpenMud/commit/c6ef5d98e90f4637cdddba7b244545fc9d52883f))

## [1.0.1](https://github.com/JeremyWildsmith/OpenMud/compare/v1.0.0...v1.0.1) (2023-10-14)


### Bug Fixes

* **build:** nr command doesnt work in runner. ([ea52431](https://github.com/JeremyWildsmith/OpenMud/commit/ea52431384337f3e0887481aacecf6b63b1b9c9d))

# 1.0.0 (2023-10-14)


### Features

* **common:** Implemented CI/CD Pipelines. ([51ed0ba](https://github.com/JeremyWildsmith/OpenMud/commit/51ed0bad7fc721dbfccec8f27a262083ed68d4c0))
