// Copyright (c) FFTSys Inc. All rights reserved.
// Use of this source code is governed by a GPL-v3 license

namespace ConsoleApp {
  using System;
  using System.Text;
  using System.Collections.Generic;

  /// <summary>
  /// Parser
  /// 
  /// </summary>
  class Parser {
    // Whether to skip experimental rules
    private bool ShouldSkipExpRule {get; set;}
    // Input string containing all Quark Rules (Legacy)
    private string RawStr {get; set;}
    // Starting tag string of a rule
    private string HeadStr { get; set; }
    // Indicates where current Rule String starts
    private int Pos { get; set; }
    private HashSet<string> ImportRefs { get; set; }

    private bool IsFirst;


    /// <summary>
    /// Constructor: init class members
    /// </summary>
    public Parser(string str, bool shouldSkipExpRule = true) {
      RawStr = str;
      HeadStr = "QuarkWprConfig(";
      Pos = 0;

      ShouldSkipExpRule = shouldSkipExpRule;
      IsFirst = true;

      ImportRefs = new HashSet<string>();
    }

    /// <summary>
    /// Verify if we have next rule
    /// </summary>
    /// <returns>
    /// Return true if next rule is available
    /// If parser reached EOF or got past all rules returns false
    /// </returns>
    /* public bool HasNext() {
      return rawStr.IndexOf(headStr, pos, rawStr.Length-pos) > 0;
    } */

    /// <summary>
    /// Get Next Rule as string
    /// Utilize position variable to find where the current rule is at.
    ///  - Start the rule from that index
    ///  - End the rule at next rule's found index
    /// </summary>
    /// <remarks>
    /// Filters enabled,
    /// - Experimental Rules
    /// - 'fuss_limited_discoverable' rules
    /// </remarks>
    /// <returns>
    /// Returns Empty string if no next rule found
    /// </returns>
    public string GetNextRule() {
      if (IsFirst) {
        IsFirst = false;
        GetNextRuleMain();
      }

      var ruleStr = GetNextRuleMain();
      if (ShouldSkipExpRule)
      {
        while ((ruleStr != string.Empty) &&
            (ruleStr.Contains("QuarkWprConfigStatus.EXPERIMENTAL") || ruleStr.Contains("fuss_limited_discoverable")))
        {
          ruleStr = GetNextRuleMain();
        }
      }
      return ruleStr;
    }


    public string GetNextRuleMain() {
      int prev = Pos;
      int current = RawStr.IndexOf(HeadStr, Pos + HeadStr.Length, RawStr.Length-Pos-HeadStr.Length);
      if (current == -1)
        return string.Empty;

      Pos = current;

      if (Pos <= prev)
        throw new System.IO.InvalidDataException($"Tag: {HeadStr} not found!");

      return RawStr.Substring(prev, Pos-prev);
    }

    /// <summary>
    /// Convert messy name to Uniform name
    /// rules to convert:
    /// 1. Prefixes
    ///  - "rule__integrity_" manually cleaned up in input file
    /// 2. Suffixes
    ///  "_via_wpr_in_quark"
    /// </summary>
    /// <returns>
    /// Given a field name in old quark rule, return its value
    /// </returns>
    private string ConvertToUniformName(string messy) {
      // Prefixes
      var cleanPrefix = messy;

      var tinyStr = "rule__";
      if (cleanPrefix.StartsWith(tinyStr))
        cleanPrefix = cleanPrefix.Substring(tinyStr.Length, cleanPrefix.Length-tinyStr.Length);  
    
      // Suffixes
      // some followd by _v2 and _exp. Hence, Replace instead of Substring
      var cleanSuffix = cleanPrefix.Replace("_via_wpr_in_quark", "");
      // some followd by _exp. Hence, Replace instead of Substring
      cleanSuffix = cleanSuffix.Replace("_wpr_quark", "");
      cleanSuffix = cleanSuffix.Replace("_in_wpr", "");
      tinyStr = "_wpr";
      if (cleanSuffix.EndsWith(tinyStr))
        cleanSuffix = cleanSuffix.Substring(0, cleanSuffix.Length-tinyStr.Length);

      // At this point, cleanSuffix has both Prefix and Suffix cleaned up


      return cleanSuffix;
    }



    /// <summary>
    /// Get Rule Name
    /// Straight forward parsing
    /// </summary>
    /// <returns>
    /// Given a field name in old quark rule, return its value
    /// </returns>
    private string GetRuleName(string haystack) {
      string needle = "ruleName=\"";
      var tailMark = "\",";

      int start = haystack.IndexOf(needle) + needle.Length;
      if (start == (needle.Length-1))
        throw new System.IO.InvalidDataException($"Tag: {needle} not found!");

      int end = haystack.IndexOf(tailMark, start + 1, haystack.Length-start-1);
      var name = haystack.Substring(start, end-start);
      name = "wpr_" + ConvertToUniformName(name);
      return SurroundStringWithQuotes(name);
    }

    // Update the indentation to match new Rules
    private string AdjustIndentation(string str) {
      return str.Replace("                   ", "       ");
    }

    public string SurroundStringWithQuotes(string str) {
      return "\"" + str + "\"";
    }


    /// <summary>
    /// `docString` 
    ///  - starts with =( or ="
    ///  - ends with )= or "=
    /// </summary>
    /// <returns>
    /// Returns desc string
    ///  - appends parenthesis around when it's multi-string
    /// isMultiLine is in correct state when input is cleaned up to not have lines like these
    ///   desc=("Boost Public posts to pos=1 for covid-19 queries."),
    /// </returns>
    private string GetRuleDescription(string haystack) {
      string needle1 = "docString=(";
      string needle2 = "docString=\"";
      var tailMark1 = "),";
      var tailMark2 = "\",";
      bool isMultiLine = true;

      int start = haystack.IndexOf(needle1) + needle1.Length;
      if (start == (needle1.Length-1)) {
        start = haystack.IndexOf(needle2) + needle2.Length;
        isMultiLine = false;
      }

      if (!isMultiLine && (start == (needle2.Length-1)))
        throw new System.IO.InvalidDataException("Tag: docString=* not found!");

      int end = haystack.IndexOf(isMultiLine? tailMark1 : tailMark2, start + 1, haystack.Length-start-1);

      var descStr = haystack.Substring(start, end-start);

      if (isMultiLine) {
        // docString specific indentation adjustment
        descStr = AdjustIndentation(descStr);
        return '(' + descStr + ')';
      }

      return SurroundStringWithQuotes(descStr);
    }

    /// <summary>
    /// Get Action Trigger Statement
    /// formats
    /// - """ statement """
    /// - " statement "
    /// - statement_variable
    /// </summary>
    private string GetActionTriggerStatement(string haystack) {
      var needles = new string[] {
          "actionTriggerStatement=\"\"\"",
          "actionTriggerStatement=\"",
          "actionTriggerStatement="};

      var tailMarks = new string[] {"\"\"\"",  "\",", ","};
      int ATSType = 0;
      int start = 0;

      for (; ATSType<3; ATSType++) {
        start = haystack.IndexOf(needles[ATSType]) + needles[ATSType].Length;

        if (start >= needles[ATSType].Length)
          break;
      }

      if (ATSType == 3)
        // throw new System.IO.InvalidDataException("Tag: docString=* not found!");
        return "";

      int end = haystack.IndexOf(tailMarks[ATSType], start + 1, haystack.Length-start-1);
      // trigger statement
      var tStat = haystack.Substring(start, end-start);
      if (ATSType == 2)
        tStat = '{' + tStat  + '}';
      return tStat;
    }

    /// <summary>
    /// Get Quark Expression
    /// Straight forward
    /// </summary>
    /// <returns>
    /// Given a field name in old quark rule, return quark exp
    /// </returns>
    private string GetQuarkExpression(string haystack, vNextRule rule) {
      string needle = "resultFilterStatement=";
      var tailMark = ",";

      int start = haystack.IndexOf(needle) + needle.Length;
      if (start == (needle.Length-1))
        throw new System.IO.InvalidDataException($"Tag: {needle} not found!");

      int end = haystack.IndexOf(tailMark, start + 1, haystack.Length-start-1);

      var quarkExpStr = haystack.Substring(start, end-start);

      var intentGuardStr = "apply_with_integrity_guard(";
      if (quarkExpStr.StartsWith(intentGuardStr)) {
        quarkExpStr = quarkExpStr.Replace(intentGuardStr, "");
        quarkExpStr = quarkExpStr.Substring(0, quarkExpStr.Length-1);
        quarkExpStr = quarkExpStr.TrimStart();
        quarkExpStr = quarkExpStr.TrimEnd(new char[] {' ', '\r', '\n'});
      }
      else {
        rule.GuardType = "IntegrityGuardType.NONE";

        if (quarkExpStr.Contains('('))
          quarkExpStr = AdjustIndentation(quarkExpStr);
      }
      
      if (ImportRefs.Add(quarkExpStr)) {
        var methodName = quarkExpStr;
        if (methodName.Contains('(')) {
          var parenthesisPosition = methodName.IndexOf('(');
          methodName = quarkExpStr.Substring(0, parenthesisPosition);
          Console.WriteLine($"{methodName},");
        }
        else
          Console.WriteLine($"{quarkExpStr},");
      }

      return quarkExpStr;
    }

    /// <summary>
    /// Join Action Trigger statement with Quark Expression
    /// </summary>
    private string CombineATSAndQuarkExp(string ActionTriggerStatement, string QuarkExpression) {
      if (ActionTriggerStatement == string.Empty || ActionTriggerStatement == "1")
        return QuarkExpression;
      return "f\"\"\"(" + ActionTriggerStatement + ") and ({" + QuarkExpression + "})\"\"\"";
    }


    /// <summary>
    /// Join Action Trigger statement with Quark Expression
    /// </summary>
    private static string GetIntegrityEnforcementTypeMap(string opStr) => opStr switch {
      "removeResult()" => "IntegrityEnforcementType.BLOCKLIST",
      "placeResultModule(100, 'result')" => "IntegrityEnforcementType.PUSH_BOTTOM",
      "placeResultModule(0, 'module')" => "IntegrityEnforcementType.BRING_TOP",
      _ => "IntegrityEnforcementType.ADDITIVE_DEMOTION",
    };

    /// <summary> 
    /// Sets
    /// - EnforcementType
    /// - OperationScript
    /// </summary>
    /// <param name="rule"> set 2 fields of this vNextRule class object </param>
    /// input examples,
    /// - resultOperationScript="moveResultModule(-5, 'result')",
    private void SetEnforcementDetails(string haystack, vNextRule rule) {
      string needle = "resultOperationScript=\"";
      var tailMark = "\",";

      int start = haystack.IndexOf(needle) + needle.Length;
      if (start == (needle.Length-1)) {
        needle = needle.Substring(0, needle.Length-1);
        start = haystack.IndexOf(needle) + needle.Length;

        if (start == (needle.Length-1))
          throw new System.IO.InvalidDataException($"Tag: {needle} not found!\r\nrule string: {haystack}");

        tailMark = tailMark.Substring(1, tailMark.Length-1);
      }

      int end = haystack.IndexOf(tailMark, start + 1, haystack.Length-start-1);
      var opStr = haystack.Substring(start, end-start);

      rule.EnforcementType = GetIntegrityEnforcementTypeMap(opStr);

      if (rule.EnforcementType == "IntegrityEnforcementType.ADDITIVE_DEMOTION")
        rule.OperationScript = opStr.Contains('(') ? $"\"{opStr}\"" : opStr;
    }

    /// <summary>
    /// Example GetFieldValue, planning to make it work a dictionary where the parsing is pretty
    /// much generic
    /// </summary>
    /// <returns>
    /// Given a field name in old quark rule, return its value
    /// </returns>
    /* private string GetFieldValue(string haystack, string needle) {
      var tailMark = ",";

      int start = haystack.IndexOf(needle) + needle.Length;
      int end = haystack.IndexOf(tailMark, start + 1, haystack.Length-start-1);
      return haystack.Substring(start, end-start);
    } */

    public string MigrateRule(string legacyRule) {
      var sb = new StringBuilder();
      var rule = new vNextRule();

      // Set each field, case by case
      rule.Name = GetRuleName(legacyRule);
      rule.Desc = GetRuleDescription(legacyRule);
      var actionTriggerStatement = GetActionTriggerStatement(legacyRule);
      var quarkExp= GetQuarkExpression(legacyRule, rule);
      rule.QuarkExpression = CombineATSAndQuarkExp(actionTriggerStatement, quarkExp);
      // rule.QuarkExpression = quarkExp;
      // Set remaining 2 fields
      SetEnforcementDetails(legacyRule, rule);

      sb.Append(rule.ToString());
      return sb.ToString();
    }

  }
}
