Select-String -Path .\Jsonata.Net.Native.TestSuite.xml -Pattern '<test-case' | Foreach {$_ -replace '^.* name="([^"]+)".*result="([^"]+)".*$', '$1;$2' } | out-file extract.txt
