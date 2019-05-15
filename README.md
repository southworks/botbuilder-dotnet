
# ![Bot Framework for dotnet](./doc/media/BotFrameworkDotnet_header.png)

### [Click here to find out what's new for //build2019!](https://github.com/Microsoft/botframework/blob/master/whats-new.md#whats-new)

# Bot Framework SDK v4 for .NET
This repository contains code for the .NET version of the [Microsoft Bot Framework SDK](https://github.com/Microsoft/botbuilder). The Bot Framework SDK v4 enable developers to model conversation and build sophisticated bot applications using .NET.

This repo is part the [Microsoft Bot Framework](https://github.com/Microsoft/botframework) - a comprehensive framework for building enterprise-grade conversational AI experiences.

 | Branch | Description        | Build Status | Coverage Status | Functional Tests Status |
 |----|---------------|--------------|-----------------|--|
 |Master | 4.5.* Preview Builds |[![Build status](https://fuselabs.visualstudio.com/SDK_v4/_apis/build/status/Bot.Builder-DotNet?branchName=master)](https://fuselabs.visualstudio.com/SDK_v4/_build/latest?definitionId=215&branchName=master) |[![Coverage Status](https://coveralls.io/repos/github/Microsoft/botbuilder-dotnet/badge.svg?branch=master)](https://coveralls.io/github/Microsoft/botbuilder-dotnet?branch=master) | [![Build status](https://fuselabs.visualstudio.com/SDK_v4/_apis/build/status/DotNet/BotBuilder-DotNet%20master%20Functional%20Tests?branchName=master)](https://fuselabs.visualstudio.com/SDK_v4/_build/latest?definitionId=484&branchName=master)
  |4.4 | 4.4.* Preview Builds |[![Build status](https://fuselabs.visualstudio.com/SDK_v4/_apis/build/status/Bot.Builder-DotNet?branchName=4.4)](https://fuselabs.visualstudio.com/SDK_v4/_build/latest?definitionId=215&branchName=4.4) |[![Coverage Status](https://coveralls.io/repos/github/Microsoft/botbuilder-dotnet/badge.svg?branch=4.4)](https://coveralls.io/github/Microsoft/botbuilder-dotnet?branch=4.4) | [![Build status](https://fuselabs.visualstudio.com/SDK_v4/_apis/build/status/DotNet/BotBuilder-DotNet%20master%20Functional%20Tests?branchName=4.4)](https://fuselabs.visualstudio.com/SDK_v4/_build/latest?definitionId=484&branchName=4.4)
   |4.3 | 4.3.* Preview Builds |[![Build status](https://fuselabs.visualstudio.com/SDK_v4/_apis/build/status/Bot.Builder-DotNet?branchName=4.3)](https://fuselabs.visualstudio.com/SDK_v4/_build/latest?definitionId=215&branchName=4.3) |[![Coverage Status](https://coveralls.io/repos/github/Microsoft/botbuilder-dotnet/badge.svg?branch=4.3)](https://coveralls.io/github/Microsoft/botbuilder-dotnet?branch=4.3) | [![Build status](https://fuselabs.visualstudio.com/SDK_v4/_apis/build/status/DotNet/BotBuilder-DotNet%20master%20Functional%20Tests?branchName=4.3)](https://fuselabs.visualstudio.com/SDK_v4/_build/latest?definitionId=484&branchName=4.3)
 |4.2 | 4.2.* Preview Builds |[![Build status](https://fuselabs.visualstudio.com/SDK_v4/_apis/build/status/Bot.Builder-DotNet?branchName=4.2)](https://fuselabs.visualstudio.com/SDK_v4/_build/latest?definitionId=215&branchName=4.2) |[![Coverage Status](https://coveralls.io/repos/github/Microsoft/botbuilder-dotnet/badge.svg?branch=4.2)](https://coveralls.io/github/Microsoft/botbuilder-dotnet?branch=4.2)
  |4.1 | 4.1.* Patch Builds |[![Build status](https://fuselabs.visualstudio.com/SDK_v4/_apis/build/status/Bot.Builder-DotNet%204.1%20CI-CD?branchName=4.1)](https://fuselabs.visualstudio.com/SDK_v4/_build/latest?definitionId=351&branchName=4.1) | [![Coverage Status](https://coveralls.io/repos/github/Microsoft/botbuilder-dotnet/badge.svg?branch=4.1)](https://coveralls.io/github/Microsoft/botbuilder-dotnet?branch=4.1)
 |4.0 | 4.0.* Patch Builds | [![Build status](https://fuselabs.visualstudio.com/SDK_v4/_apis/build/status/Bot.Builder-DotNet-4.0%20Branch?branchName=4.0)](https://fuselabs.visualstudio.com/SDK_v4/_build/latest?definitionId=341) | N/A |

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/Microsoft/botbuilder-dotnet/blob/master/LICENSE)
[![Gitter](https://img.shields.io/gitter/room/Microsoft/BotBuilder.svg)](https://gitter.im/Microsoft/BotBuilder)

[![StackExchange](https://img.shields.io/stackexchange/stackoverflow/t/botframework.svg)](https://stackoverflow.com/questions/tagged/botframework)



In addition to the .NET SDK, Bot Builder supports creating bots in other popular programming languages like [JavaScript](https://github.com/Microsoft/botbuilder-js), [Python (Preview)](https://github.com/Microsoft/botbuilder-python), and [Java (Preview)](https://github.com/Microsoft/botbuilder-java).

To get started see the [Azure Bot Service Documentation](https://docs.microsoft.com/en-us/azure/bot-service/?view=azure-bot-service-4.0) for the v4 SDK.

The [Bot Framework Samples](https://github.com/microsoft/botbuilder-samples) includes a rich set of samples repository.


## Packages
| Name                                  | Released Package | Daily Build                                                                                                                                                                  |
|---------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------|--|
| Microsoft.Bot.Builder                 | [![BotBuilder Badge](https://buildstats.info/nuget/Microsoft.Bot.Builder?dWidth=70)](https://www.nuget.org/packages/Microsoft.Bot.Builder/)                                 | [![BotBuilder Badge](https://buildstats.info/myget/botbuilder/botbuilder-v4-dotnet-daily/Microsoft.Bot.Builder?includePreReleases=true&dWidth=50)](https://botbuilder.myget.org/feed/botbuilder-v4-dotnet-daily/package/nuget/Microsoft.Bot.Builder) |
| Microsoft.Bot.Builder.AI.LUIS         | [![BotBuilder Badge](https://buildstats.info/nuget/Microsoft.Bot.Builder.AI.LUIS?dWidth=70)](https://www.nuget.org/packages/Microsoft.Bot.Builder.AI.LUIS/)                 | [![BotBuilder Badge](https://buildstats.info/myget/botbuilder/botbuilder-v4-dotnet-daily/Microsoft.Bot.Builder.AI.LUIS?includePreReleases=true&dWidth=50)](https://botbuilder.myget.org/feed/botbuilder-v4-dotnet-daily/package/nuget/Microsoft.Bot.Builder.AI.LUIS) |
| Microsoft.Bot.Builder.AI.QnA          | [![BotBuilder Badge](https://buildstats.info/nuget/Microsoft.Bot.Builder.AI.QnA?dWidth=70)](https://www.nuget.org/packages/Microsoft.Bot.Builder.AI.Qna/)                   | [![BotBuilder Badge](https://buildstats.info/myget/botbuilder/botbuilder-v4-dotnet-daily/Microsoft.Bot.Builder.AI.QnA?includePreReleases=true&dWidth=50)](https://botbuilder.myget.org/feed/botbuilder-v4-dotnet-daily/package/nuget/Microsoft.Bot.Builder.AI.QnA) |
| Microsoft.Bot.Builder.Azure           | [![BotBuilder Badge](https://buildstats.info/nuget/Microsoft.Bot.Builder.Azure?dWidth=70)](https://www.nuget.org/packages/Microsoft.Bot.Builder.Azure/)                     | [![BotBuilder Badge](https://buildstats.info/myget/botbuilder/botbuilder-v4-dotnet-daily/Microsoft.Bot.Builder.Azure?includePreReleases=true&dWidth=50)](https://botbuilder.myget.org/feed/botbuilder-v4-dotnet-daily/package/nuget/Microsoft.Bot.Builder.Azure) |
| Microsoft.Bot.Builder.Dialogs         | [![BotBuilder Badge](https://buildstats.info/nuget/Microsoft.Bot.Builder.Dialogs?dWidth=70)](https://www.nuget.org/packages/Microsoft.Bot.Builder.Dialogs/)                 | [![BotBuilder Badge](https://buildstats.info/myget/botbuilder/botbuilder-v4-dotnet-daily/Microsoft.Bot.Builder.Dialogs?includePreReleases=true&dWidth=50)](https://botbuilder.myget.org/feed/botbuilder-v4-dotnet-daily/package/nuget/Microsoft.Bot.Builder.Dialogs) |
| Microsoft.Bot.Builder.TemplateManager | [![BotBuilder Badge](https://buildstats.info/nuget/Microsoft.Bot.Builder.TemplateManager?dWidth=70)](https://www.nuget.org/packages/Microsoft.Bot.Builder.TemplateManager/) | [![BotBuilder Badge](https://buildstats.info/myget/botbuilder/botbuilder-v4-dotnet-daily/Microsoft.Bot.Builder.TemplateManager?includePreReleases=true&dWidth=50)](https://botbuilder.myget.org/feed/botbuilder-v4-dotnet-daily/package/nuget/Microsoft.Bot.Builder.TemplateManager) |
| Microsoft.Bot.Builder.Integration.ApplicationInsights.Core | [![BotBuilder Badge](https://buildstats.info/nuget/Microsoft.Bot.Builder.Integration.ApplicationInsights.Core?dWidth=70)](https://www.nuget.org/packages/Microsoft.Bot.Builder.Integration.ApplicationInsights.Core/) | [![BotBuilder Badge](https://buildstats.info/myget/botbuilder/botbuilder-v4-dotnet-daily/Microsoft.Bot.Builder.Integration.ApplicationInsights.Core?includePreReleases=true&dWidth=50)](https://botbuilder.myget.org/feed/botbuilder-v4-dotnet-daily/package/nuget/Microsoft.Bot.Builder.Integration.ApplicationInsights.Core) |
| Microsoft.Bot.Builder.Integration.ApplicationInsights.WebApi | [![BotBuilder Badge](https://buildstats.info/nuget/Microsoft.Bot.Builder.Integration.ApplicationInsights.WebApi?dWidth=70)](https://www.nuget.org/packages/Microsoft.Bot.Builder.Integration.ApplicationInsights.WebApi/) | [![BotBuilder Badge](https://buildstats.info/myget/botbuilder/botbuilder-v4-dotnet-daily/Microsoft.Bot.Builder.Integration.ApplicationInsights.WebApi?includePreReleases=true&dWidth=50)](https://botbuilder.myget.org/feed/botbuilder-v4-dotnet-daily/package/nuget/Microsoft.Bot.Builder.Integration.ApplicationInsights.WebApi) |
| Microsoft.Bot.Configuration           | [![BotBuilder Badge](https://buildstats.info/nuget/Microsoft.Bot.Configuration?dWidth=70)](https://www.nuget.org/packages/Microsoft.Bot.Configuration/)                     | [![BotBuilder Badge](https://buildstats.info/myget/botbuilder/botbuilder-v4-dotnet-daily/Microsoft.Bot.Configuration?includePreReleases=true&dWidth=50)](https://botbuilder.myget.org/feed/botbuilder-v4-dotnet-daily/package/nuget/Microsoft.Bot.Configuration) |
| Microsoft.Bot.Connector               | [![BotBuilder Badge](https://buildstats.info/nuget/Microsoft.Bot.Connector?dWidth=70)](https://www.nuget.org/packages/Microsoft.Bot.Connector/)                             | [![BotBuilder Badge](https://buildstats.info/myget/botbuilder/botbuilder-v4-dotnet-daily/Microsoft.Bot.Connector?includePreReleases=true&dWidth=50)](https://botbuilder.myget.org/feed/botbuilder-v4-dotnet-daily/package/nuget/Microsoft.Bot.Connector) |
| Microsoft.Bot.Schema                  | [![BotBuilder Badge](https://buildstats.info/nuget/Microsoft.Bot.Schema?dWidth=70)](https://www.nuget.org/packages/Microsoft.Bot.Schema/)                                   | [![BotBuilder Badge](https://buildstats.info/myget/botbuilder/botbuilder-v4-dotnet-daily/Microsoft.Bot.Schema?includePreReleases=true&dWidth=50)](https://botbuilder.myget.org/feed/botbuilder-v4-dotnet-daily/package/nuget/Microsoft.Bot.Schema) |

To use the daily builds, which are published to MyGet, please follow the instructions [here](UsingMyGet.md).


## Contributing
This project welcomes contributions and suggestions. Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.
When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.
This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Reporting Security Issues
Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC) at [secure@microsoft.com](mailto:secure@microsoft.com). You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the [MSRC PGP](https://technet.microsoft.com/en-us/security/dn606155) key, can be found in the [Security TechCenter](https://technet.microsoft.com/en-us/security/default).

Copyright (c) Microsoft Corporation. All rights reserved.

