# Instructions for Consumers

Load this [Postman collection](../master/postmanExample.json) to see the endpoint and required headers.

To use version 2 of the API, include a header `version` with value `v2` in the request.

API version 1 returns json like:

```json
[
    {
        "currentInForceSopConditionName": "animal envenomation",
        "confidencePercentage": 98
    },
    {
        "currentInForceSopConditionName": "physical injury due to munitions discharge",
        "confidencePercentage": 39
    },
    {
        "currentInForceSopConditionName": "None",
        "confidencePercentage": 17
    },
    {
        "currentInForceSopConditionName": "substance use disorder",
        "confidencePercentage": 6
    },
    {
        "currentInForceSopConditionName": "decompression sickness",
        "confidencePercentage": 4
    }
]
```

API version 2 returns additional flags:

```json

{
    "flags": [
        {
            "name": "multipleConditions",
            "description": "The user's description of their condition may cover more than one SoP condition.",
            "value": true,
            "reason": "The description of the diagnosis contained conjunctions."
        }
    ],
    "conditions": [
        {
            "currentInForceSopConditionName": "lumbar spondylosis",
            "confidencePercentage": 100
        },
        {
            "currentInForceSopConditionName": "intervertebral disc prolapse",
            "confidencePercentage": 62
        },
        {
            "currentInForceSopConditionName": "thoracic spondylosis",
            "confidencePercentage": 5
        },
        {
            "currentInForceSopConditionName": "alcohol use disorder",
            "confidencePercentage": 4
        },
        {
            "currentInForceSopConditionName": "physical injury due to munitions discharge",
            "confidencePercentage": 3
        }
    ]
}
```






