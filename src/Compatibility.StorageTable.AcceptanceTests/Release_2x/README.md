We looked at several solutions like

- Referencing the previous nuget package leveraging aliasing
- Using a dedicated repository to verify backward compatibility
- Spinning up custom app domains that load different assemblies

One of the main goals was to make sure we are backward compatible and detect it early on master so a dedicated repository was ruled out. Custom aliasing didn't work out in the same acceptance test project and spinning up custom app domains was deemed too complex so we settled with a tradeoff of copying the previous saga persister code into master. In order for the code to not trigger analysis the files have been treated as external sources and use `.g.cs` as a file extension.

The sources used only need to be updated if a patch version on the 2.4 branch introduced changes to the `DictionaryTableEntity` or the secondary index algorithm.