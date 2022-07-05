// Copyright (c) FFTSys Inc. All rights reserved.
// Use of this source code is governed by a GPL-v3 license

namespace Parser {
  using System.Threading.Tasks;
  using System.CommandLine;

  class MTDemo {
    /// <summary>
    /// Entry Point Class
    /// Support various types of migration
    /// Input/Output files should come from JSON input file
    /// TODO: outputType and shouldGenerateParams seem to be overlapping
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    static async Task Main(string[] args) {
      var claApp = new vNextMT();

      var rootCmd = new RootCommand();
      var verticalOption = new Option<string>(new[] {"--vertical", "-vt"}, "Specify vertical for generating rules");
      rootCmd.AddOption(verticalOption);
      var agRulesOption = new Option<bool>(new[] {"--autogen", "-ag"}, "Whether we are parsing tool generated rules file i.e., output rule file of this tool.");
      rootCmd.AddOption(agRulesOption);
      var paramsOption = new Option<bool>(new[] {"--getParams", "-p"}, "Generate request params only");
      rootCmd.AddOption(paramsOption);

      rootCmd.SetHandler<string, bool, bool>(
        async (vertical, isAutoGenRules, shouldGenerateParams) => {
          var appSettings = new AppSettings();

          // read and set command line args
          appSettings.SetCLArgs(
            vertical?? "WPR",
            isAutoGenRules,
            shouldGenerateParams
          );


          // read json and set rest of the properties 
          // await appSettings.LoadJson();

          await claApp.Run(appSettings);
        },
        verticalOption,
        agRulesOption,
        paramsOption
      );

      await rootCmd.InvokeAsync(args);
    }
  }
}
