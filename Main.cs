// Copyright (c) FFTSys Inc. All rights reserved.
// Use of this source code is governed by a GPL-v3 license

namespace ConsoleApp {
  using System;
  using System.Threading.Tasks;

  class MTDemo {
    /// <summary>
    /// Entry Point Class
    /// Support various type of migrations
    /// Input/Output files should come from JSON input file
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    static async Task Main(string[] args) {
      var app = new vNextMT();
      await app.Run();
    }
  }
}
