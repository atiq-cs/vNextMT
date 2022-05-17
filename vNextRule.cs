// Copyright (c) FFTSys Inc. All rights reserved.
// Use of this source code is governed by a GPL-v3 license

namespace ConsoleApp {
  using System.Text;
  using System.Collections.Generic;

  /// <summary>
  /// vNextRule
  /// </summary>
  class vNextRule {
    private string Head {get; set;}

    /// <summary>
    /// Fields aligned with `enforcement_rules/integrity_enforcement.thrift`
    /// </summary>
    public string Name {get; set;}
    public string Desc { get; set; }
    public string QuarkExpression { get; set; }
    public string EnforcementType { get; set; }
    public string OperationScript { get; set; }

    private string Tail {get; set;}
    // This should be set by the python source file: wpr_migration_rules file
    // public string RankingStage { get; set; }

    /// <summary>
    /// Constructor: init class members
    /// </summary>
    public vNextRule() {
      Head = "RuleInfo(";
      Tail = ")";

      OperationScript = string.Empty;
    }

    private string Format(string str, int indentAmount = 2, bool isFirstLine=false) {
      var indent = string.Empty;
      for (int i=0; i<indentAmount; i++)
        indent += "    ";
      return indent + str + (isFirstLine? string.Empty : ',') + System.Environment.NewLine;
    }


    public override string ToString()
    {
      return Format(Head, 1, true)
        + Format($"name={Name}")
        + Format($"desc={Desc}")
        + Format($"quark_exp={QuarkExpression}")
        + Format($"enforcement_type={EnforcementType}")
        + (OperationScript == string.Empty? string.Empty : Format($"op_str={OperationScript}"))
        + Format(Tail, 1);
    }
  }
}
