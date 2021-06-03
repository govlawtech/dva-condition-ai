param (
    [Parameter(Mandatory=$true)]
    [string]$condition
)
Invoke-WebRequest -Uri "http://localhost:7071/api/suggestSoPConditionInternal?conditionAsDescribedByUser=$condition" -Headers @{'Accept' = 'application/json; charset=utf-8'}