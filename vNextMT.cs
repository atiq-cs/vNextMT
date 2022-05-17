// Copyright (c) FFTSys Inc. All rights reserved.
// Use of this source code is governed by a GPL-v3 license

namespace ConsoleApp {
  using System.IO;
  using System.Text;
  using System.Threading.Tasks;

  /// <summary>
  /// vNext Migration Tool
  /// Main Class
  /// </summary>
  class vNextMT {
     /// <summary>
    /// Constructor: sets first 5 properties
    /// </summary>
    public vNextMT() {}


    /// <summary>
    /// Run automation for the app
    /// </summary>
    public async Task Run() {
      var legacyRulesFilePath = @"D:\Doc\Search\vNext_Migration\integrity_configlist.cinc";
      var vNextRuleFilesPath = @"D:\Doc\Search\vNext_Migration\vNext_Rules.py";

      var fileContent = File.ReadAllText(legacyRulesFilePath);
      var parser = new Parser(fileContent);

      string legacyRule = string.Empty;
      var sb = new StringBuilder();

      while( (legacyRule = parser.GetNextRule() ) != string.Empty) {
        var rule = parser.MigrateRule(legacyRule);
        sb.Append(rule);
      }

      await File.WriteAllTextAsync(vNextRuleFilesPath, sb.ToString());
   }
  }
}
