### 🔍 Search Results: `{{Request.Query}}`

{{#if ErrorMessage}}
**Error:** {{ErrorMessage}}
{{else}}
**Found {{Results.length}} of {{TotalResults}} results**

{{#if Results}}
{{#each Results}}
#### {{@index}}. {{Name}} (`{{Type}}` - {{SimilarityScore}})
**{{Description}}**  
**URL:** {{Url}}  
**Match:** {{MatchedUtterance}}  
**Context:** {{Context.PageName}}{{#if Context.ElementName}} > {{Context.ElementName}}{{/if}}{{#if Context.TaskName}} > {{Context.TaskName}}{{/if}}

{{/each}}
{{else}}
**No results found**
{{/if}}
{{/if}}
