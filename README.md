# GatoGPT - Manage and interact with your local LLMs
The main aim for `GatoGPT` is to provide an easy and hassle free experience to run and interact programatically with your LLMs (Large Language Models), while still providing the required customisation and configurations abilities required to run them.

**NOTE: Project is an on-going WIP and testing / feedback is appreciated**

## OpenAI API Endpoints
The following endpoints are available:

- [x] `/v1/models` && `/v1/models/{model}`Models API
- [x] `/v1/completions` Completions API (legacy)
- [x] `/v1/chat/completions` Chat Completions API
    - Includes support for Tool Calling.
    - Includes support for LLaVA models acting as Vision inference.
    
    Note: requires `InferenceParams.ImageModelId` to be set on the Model Definition to a compatible LLaVA Model Definition
- [x] `/v1/embeddings` Embeddings API

## Required Libraries
At present 2 other raw code libraries are required to be in a `libs/` directory. Entries for these already exist in `.gitignore` and can be safely cloned to the respective folders:

```sh
  git clone git@github.com:elgatopanzon/godotegp-framework.git libs/godotegp-framework
  git clone git@github.com:elgatopanzon/godotegp-framework-nongame.git libs/godotegp-framework-nongame
```

In addition to this, the project depends on `llama.cpp` and `llama.cpp-server` processes being available when using the `LlamaCpp` and `LlamaCppServer` backends for Text Generation models.

The Dockerfile and docker-compose.yml takes this into account already when building the image and is only required for building the image without Docker.

## Getting Started
The following details how to get up and running with GatoGPT.

Note: the `user://` path is the application's user data folder which on Linux is `$HOME/.local/share/GatoGPT_Data`. Models can be stored anywhere on the filesystem, but for ease of use it's recommended to store them in `user://Models`.

### Configure model files as Model Resources
Model files (such as GGUF) are added to the configuration as a Model Resource. Each resource has a friendly ID and a Path as well as a Class name, which defines the type of resource. Resources are loaded by the server and used to refer to specific items using a friendly ID rather than a direct filepath.

An example Model Resource configuration file defining 2 models, located at
`user://Config/GodotEGP.Config.ResourceDefinitionConfig/Config.json`:

```json
  {
    "Resources": {
      "LlamaModels": {
        "TheBloke/Orca-2-13B-GGUF/Q6_K": {
          "Path": "user://Models/TheBloke/Orca-2-13B-GGUF/orca-2-13b.Q6_K.gguf",
          "Class": "GatoGPT.Resource.LlamaModel"
        },
        "TheBloke/Mistral-7B-Instruct-v0.2-GGUF/Q5_K_M": {
          "Path": "user://Models/TheBloke/Mistral-7B-Instruct-v0.2-GGUF/mistral-7b-instruct-v0.2.Q5_K_M.gguf",
          "Class": "GatoGPT.Resource.LlamaModel"
        }
      }
    }
  }
  
```

The resource `TheBloke/Mistral-7B-Instruct-v0.2-GGUF/Q5_K_M` now refers to the `Q5_K_M` quantisation of this model and can be referenced in Model Definitions.

### Define models as Model Definitions
To create a model to be accessed by the API, a Model Definition must be created. The definition includes the following configurable options:

- `Id` the ID of the model that is used in the API call, e.g. `mistral-7b-instruct`
- `ModelResourceId` the friendly name of our model resource, e.g. `TheBloke/Mistral-7B-Instruct-v0.2-GGUF/Q5_K_M`.
Note: don't use the filepath for the model definition.
- `OwnedBy` defaults to local, but can be defined as whatever you like
- `Backend` the backend to use, available options for Text Generation are:
    - `BuiltinLlama` uses the built-in inference engine powered by Llama.cpp (may not support some newer models).
    - `LlamaCppServer` uses the server offered by `llama.cpp`.
        - Note: requires the process `llama.cpp-server` when running outside Docker.
    - `LlamaCpp` calls the `llama.cpp` process directly (not recommended).
        - Note: requires the process `llama.cpp` when running outside Docker.
    - `openai` OpenAI passthrough mode, only works with OpenAI models.
- `ProfilePreset` and `ModelProfileOverride` see below.

### Model Profiles and profile presets
Each Model Definition allows full customisation of load parameters and inference parameters.
Model Profiles can be defined in a json file at `user://Config/GatoGPT.Config.ModelDefinitionsConfig/Config.json`.

```json
  {
    "TextGeneration": {
      "mistral-7b-instruct": {
        "ModelResourceId": "TheBloke/Mistral-7B-Instruct-v0.2-GGUF/Q5_K_M"
        }
      },
      "orca-2-13b": {
        "ModelResourceId": "TheBloke/Orca-2-13B-GGUF/Q6_K",
        "ProfilePreset": "chatml"
        }
      }
    }
  }
  
```

The above is an example configuration defining 2 models, `mistral-7b-instruct` and `orca-2-13b`. These models reference the Model Resources created in the previous example. This allows easily creating multiple models using the same underlying model file but defining different parameters.

#### Automatic profile detection
By default an attempt to choose the default profile preset is done by the name of the model resource. E.g. `Mistral-7B-Instruct-v0.2-GGUF` maps to the `mistral_instruct` profile preset automatically configuring the model's prompt format without extra requirements (input prefix/suffix, default system prompt, etc).

If this process fails the profile will be set to a default profile based on the `Alpaca` format.

#### Setting the profile manually
The profile type can be set directly on the Model Definition as `ProfilePreset`, which is useful when you know the model's prompt format but the automatic filename detection does not map to the profile. This is done in the example above setting the `orca-2-13b` model to use the `chatml` preset.

```json
  "ProfilePreset": "chatml"
```

The default profiles are located in the `Config` directory of the code base, and can be created manually by adding them in a json file inside `user://Config/GatoGPT.Config.TextGenerationPresetsConfig/ModelProfiles.json`.

#### Defining your own parameters
Parameters can be set in the model definition using `ModelProfileOverride`. These overrides replace the configuration applied by the profile preset.

Below is a full example of configuring each of the LoadParams and InferenceParams settings for a model, in this case `mistral-7b-instruct`:
```json
  {
    "TextGeneration": {
      "mistral-7b-instruct": {
        "ModelResourceId": "TheBloke/Mistral-7B-Instruct-v0.2-GGUF/Q5_K_M",
        "Backend": "LlamaCppServer",
        "OwnedBy": "mistral",
        "ModelProfileOverride": {
          "LoadParams": {
            "NCtx": 16384,
            "NBatch": 512,
            "RopeFreqBase": 0,
            "RopeFreqScale": 0,
            "NGpuLayers": 0,
            "UseMlock": false,
            "MainGpu": 0,
            "Seed": 1337,
            "F16KV": true,
            "UseMMap": true,
            "KVOffload": false,
            "MMProjPath": "/path/to/mmproj.bin"
          },
          "InferenceParams": {
            "NThreads": 5,
            "KeepTokens": 0,
            "NPredict": 100,
            "TopK": 40,
            "MinP": 0.05,
            "TopP": 0.95,
            "Temp": 0.8,
            "RepeatPenalty": 1.1,
            "FrequencyPenalty": 0,
            "PresencePenalty": 0,
            "Tfs": 1.0,
            "Typical": 1.0,
            "RepeatLastN": 64,
            "AntiPrompts": ["[INST]"],
            "InputPrefix": "[INST]",
            "InputSuffix": "[/INST]",
            "PrePrompt": "",
            "PrePromptPrefix": "",
            "PrePromptSuffix": "",
            "InstructTemplate": "{{ PrePromptPrefix }}{{ PrePrompt }}{{ PrePromptSuffix }}{{ InputPrefix }}{{ Input }}{{ InputSuffix }}",
            "ChatTemplate": "{{ PrePromptPrefix }}{{ PrePrompt }}{{ PrePromptSuffix }}{{ InputPrefix }}{{ Input }}{{ InputSuffix }}",
            "ChatMessageTemplate": "{{ Name }}: {{ Message }}",
            "ChatMessageGenerationTemplate": "{{ AssistantName }}: ",
            "TemplateType": "instruct",
            "ImageModelId": "testmodel-image"
          }
        }
      }
    }
  }
  
```

### Generating Text on the Command Line
The command-line interface can be used to view and verify your defined models and generate text.

The command `dotnet run models` Lists model resources and model definitions.

Example output:
```
  Model Resources:
  
  # TheBloke/Orca-2-13B-GGUF/Q6_K #
  Path: user://Models/TheBloke/Orca-2-13B-GGUF/orca-2-13b.Q6_K.gguf
  
  # TheBloke/Mistral-7B-Instruct-v0.2-GGUF/Q5_K_M #
  Path: user://Models/TheBloke/Mistral-7B-Instruct-v0.2-GGUF/mistral-7b-instruct-v0.2.Q5_K_M.gguf
  
  Model Definitions:
  # mistral-7b-instruct #
  Model Resource: TheBloke/Mistral-7B-Instruct-v0.2-GGUF/Q5_K_M
  Automatic Preset: Mistral Instruct
  Profile Preset Override:
  Backend: BuiltinLlama
  Model Profile:
  {
    "LoadParams": {
      "NCtx": 16384,
      "NBatch": 512,
      "RopeFreqBase": 0.0,
      "RopeFreqScale": 0.0,
      "NGpuLayers": 0,
      "UseMlock": false,
      "MainGpu": 0,
      "Seed": -1,
      "F16KV": true,
      "UseMMap": true,
      "KVOffload": true,
      "MMProjPath": ""
    },
    "InferenceParams": {
      "NThreads": 5,
      "KeepTokens": 0,
      "NPredict": -1,
      "TopK": 40,
      "MinP": 0.05,
      "TopP": 0.95,
      "Temp": 0.8,
      "RepeatPenalty": 1.1,
      "FrequencyPenalty": 0.0,
      "PresencePenalty": 0.0,
      "Tfs": 1.0,
      "Typical": 1.0,
      "RepeatLastN": 64,
      "Antiprompts": [
        "### Instruction:",
        "[INST]"
      ],
      "InputPrefix": "[INST]",
      "InputSuffix": "[/INST]",
      "PrePrompt": "Below is an instruction that describes a task. Write a response that appropriately completes the request.",
      "PrePromptPrefix": "",
      "PrePromptSuffix": "",
      "ImagePath": "",
      "ImageModelId": "testmodel-image",
      "InstructTemplate": "{{ PrePromptPrefix }}{{ PrePrompt }}{{ PrePromptSuffix }}{{ InputPrefix }}{{ Input }}{{ InputSuffix }}",
      "ChatTemplate": "{{ PrePromptPrefix }}{{ PrePrompt }}{{ PrePromptSuffix }}{{ InputPrefix }}{{ Input }}{{ InputSuffix }}",
      "ChatMessageTemplate": "{{ Name }}: {{ Message }}",
      "ChatMessageGenerationTemplate": "{{ AssistantName }}: ",
      "TemplateType": "instruct"
    },
    "Name": "Mistral Instruct"
  }
  
  # orca-2-13b #
  Model Resource: TheBloke/Orca-2-13B-GGUF/Q6_K
  Automatic Preset: ChatML
  Profile Preset Override: chatml
  Backend: BuiltinLlama
  Model Profile:
  {
    "LoadParams": {
      "NCtx": 2048,
      "NBatch": 512,
      "RopeFreqBase": 0.0,
      "RopeFreqScale": 0.0,
      "NGpuLayers": 0,
      "UseMlock": false,
      "MainGpu": 0,
      "Seed": -1,
      "F16KV": true,
      "UseMMap": true,
      "KVOffload": true,
      "MMProjPath": ""
    },
    "InferenceParams": {
      "NThreads": 5,
      "KeepTokens": 0,
      "NPredict": -1,
      "TopK": 40,
      "MinP": 0.05,
      "TopP": 0.95,
      "Temp": 0.8,
      "RepeatPenalty": 1.1,
      "FrequencyPenalty": 0.0,
      "PresencePenalty": 0.0,
      "Tfs": 1.0,
      "Typical": 1.0,
      "RepeatLastN": 64,
      "Antiprompts": [
        "### Instruction:",
        "<|im_start|>",
        "<|im_end|>"
      ],
      "InputPrefix": "<|im_start|>user\n",
      "InputSuffix": "\n<|im_end|>\n<|im_start|>assistant\n",
      "PrePrompt": "Perform the task to the best of your ability.",
      "PrePromptPrefix": "<|im_start|>system\n",
      "PrePromptSuffix": "<|im_end|>\n",
      "ImagePath": "",
      "ImageModelId": "testmodel-image",
      "InstructTemplate": "{{ PrePromptPrefix }}{{ PrePrompt }}{{ PrePromptSuffix }}{{ InputPrefix }}{{ Input }}{{ InputSuffix }}",
      "ChatTemplate": "{{ PrePromptPrefix }}{{ PrePrompt }}{{ PrePromptSuffix }}{{ Input }}",
      "ChatMessageTemplate": "<|im_start|>{{ Role }} {{ Name }}:\n{{ Message }}\n<|im_end|>",
      "ChatMessageGenerationTemplate": "<|im_start|>assistant {{ AssistantName }}:\n",
      "TemplateType": "instruct"
    },
    "Name": "ChatML"
  }
  
```

Using the `generate` command we can start generating text with one of the defined models:

```sh
  $ dotnet run generate --model orca-2-13b --prompt "Was the dog really a lazy dog just because the brown fox was quick?" --log-level Info
  ...
  The dog's laziness cannot be determined solely based on the comparison to the quick brown fox. Dogs have different personalities and energy levels, so it is important not to make assumptions about a dog's character based on their interactions with other animals.
  GenerationTime: 26002.657 ms
  PromptTokenCount: 73
  GeneratedTokenCount: 56
  TotalTokenCount: 129
  TimeToFirstToken: 6966.003 ms
  TokensPerSec: 2.9416934299483515
```

## Starting the OpenAI API Server (with Docker)
The included Dockerfile prepares the required environment to run the API, and the docker-compose.yml provides a good starting point for mounting the required directories.

To build the Docker image: `docker build -t elgatopanzon/gatogpt:latest .
--no-cache`

To run the Docker image: `docker run -p 8181:8181 -v ./Config:/root/.local/share/GatoGPT_Data/Config elgatopanzon/gatogpt:latest`

To run using Docker Compose with the included example which also passes in an NVIDIA GPU: 
- `docker compose build --no-cache` to build the image.
- `docker compose up` to start the API server.

### Notes about Backends using Docker deployment
At present, the Dockerfile builds llama.cpp using CUDA, which removes support for CPU inference. This affects the backends `LlamaCpp` and `LlamaCppServer`. This means that the only backend supporting CPU inference is `BuiltinLlama` which is the default backend when the key is not specific in a Model Definition.

To enable GPU support for `BuiltinLlama`, it must be enabled in the `GatoGPT.csproj` by commenting out the `Cpu` backend and uncommenting the `Cuda11` and `Cuda12` backends and re-building the docker image with `--no-cache`.

## Starting the OpenAI API server (without Docker)
Launching the API is done with the following command: `dotnet run api --host 0.0.0.0 --port 8181`.

Once launched, it can be verified by issuing a call to the Models API endpoint. This also verifies that the server is configured correctly with Model Definitions.

```sh
  $ curl http://localhost:8181/v1/models -H "Authorization: Bearer no-key"
```

This call produces the following result listing the available model definitions:
```json
  {
    "data": [
      {
        "id": "mistral-7b-instruct",
        "created": 1705459662,
        "owned_by": "local",
        "object": "model"
      },
      {
        "id": "orca-2-13b",
        "created": 1705459662,
        "owned_by": "local",
        "object": "model"
      }
    ],
    "object": "list"
  }
```

## Tool Calling support for Chat Completion API
The Chat Completions API supports Tool Calling by letting the model decide if it should call one or more provided tools, or simply respond to the provided prompt without calling any tools. This is achieved by implementing a "respond" tool which the model can pick from the provided list when it just wants to reply normally without calling a tool. This allows the `tool_choice: auto` to work, leaving tool calling up to the model.

The implementation is still considered unreliable and depends on the model used and whether or not it is inteligent enough to call a single tool or multiple tools with the correct output. Since it does not currently use Grammars, smaller models such as 1-3Bs don't tend to pickup the intention and output the call using the example data. 7B and above has much higher chances of producing a tool call.

Below is an example API request contining 2 user messages, a tool example based on OpenAI's `get_current_weather` example which is extended and intended to prompt the model to create 2 separate outputs.

```sh
$ curl http://localhost:8181/v1/chat/completions --silent \
-H "Content-Type: application/json" \
-H "Authorization: Bearer no-key" \
-d '{
  "model": "mistral-7b-instruct",
  "messages": [
    {
      "role": "user",
      "content": "What is the weather in New York?"
    },     {
      "role": "user",
      "content": "How are you feeling today?"
    }

  ],
  "tools": [
    {
      "type": "function",
      "function": {
        "name": "get_current_weather",
        "description": "Get the current weather in a given location",
        "parameters": {
          "type": "object",
          "properties": {
            "location": {
              "type": "string",
              "description": "The city and state, e.g. San Francisco, CA"
            },
            "unit": {
              "type": "string",
              "enum": ["celsius", "fahrenheit"]
            }
          },
          "required": ["location"]
        }
      }
    }
  ],
  "tool_choice": "auto", "temperature": 0, "seed": 1337
}'
```

While using `mistral-7b-instruct`, it ignores the second message and instead outputs the tool call to get the current weather:

```json
{
  "id": "cmpl-27083738-52034763-66675449",
  "choices": [
    {
      "message": {
        "content": null,
        "role": "assistant",
        "tool_calls": [
          {
            "id": "toolcall-27083738-44490652",
            "type": "function",
            "function": {
              "name": "get_current_weather",
              "arguments": "{\"location\":\"New York\"}"
            }
          }
        ]
      },
      "finish_reason": "tool_call",
      "index": 0,
      "logprobs": null,
      "inference_result": {
        ...
        "output": " I'm an AI and don't have feelings, but I can help you check the weather in New York. Here's how:\n\n{ \"function\": \"get_current_weather\", \"arguments\": {\"location\": \"New York\"}}",
      }
    }
  ],
  ...
}

```

The response is stripped out, but as you can see from the `inference_result.output` object (which is an extra output unique to GatoGPT), the model *did* infact answer the second query, just not in the correct format of a tool call.

Providing the same API call to `orca-2-13b` on the other hand gives the opposite. It ignores the weather request, and instead correctly responds using the provided `respond` function:

```json
{
  "id": "cmpl-51928983-61492238-60329649",
  "choices": [
    {
      "message": {
        "content": "I'm feeling fine, thank you for asking.",
        "role": "assistant",
        "tool_calls": [
          {
            "id": "toolcall-51928983-32883419",
            "type": "function",
            "function": {
              "name": "respond",
              "arguments": "{\"response\":\"I'm feeling fine, thank you for asking.\"}"
            }
          }
        ]
      },
      "finish_reason": "stop",
      "index": 0,
      "logprobs": null,
      "inference_result": {
        ...
        "output": "{ \"function\": \"respond\", \"arguments\": {\"response\": \"I'm feeling fine, thank you for asking.\"}}",
      }
    }
  ],
  ...
}

```

The `respond` function only exists when the API call includes a list of tools. This puts the model and prompt into "function mode" where it's expected to output parsable JSON objects. The `respond` function is internally parsed to provide a normal response in the `content` output, while including the tool call object for the function call.

## Vision/MM Models for Chat Completion API
Vision capability works by internally requesting image identification from a compatible LLaVA model, then returning the result to the calling model. This opens up the capability for *any* model to gain Vision support in an OpenAI compatible way!

To give models Vision capabilities, first define a Model Definition, and set the `ModelProfileOverride.InferenceParams.ImageModelId` setting to the ID of a LLaVA MM model.

In the following example, a Model Definition exists for `bakllava-1` and is configured on the `mistral-7b-instruct` and `orca-2-13b` models:
```json
  {
    "TextGeneration": {
      "mistral-7b-instruct": {
        "ModelResourceId": "TheBloke/Mistral-7B-Instruct-v0.2-GGUF/Q5_K_M",
        "ModelProfileOverride": {
          "InferenceParams": {
            "ImageModelId": "bakllava-1"
          }
        }
      },
      "orca-2-13b": {
        "ModelResourceId": "TheBloke/Orca-2-13B-GGUF/Q6_K",
        "ProfilePreset": "chatml",
        "ModelProfileOverride": {
          "InferenceParams": {
            "NThreads": 5,
            "ImageModelId": "bakllava-1"
          }
        }
      },
      "bakllava-1": {
        "ModelResourceId": "abetlen/BakLLaVA-1-GGUF/Q5_K",
        "Backend": "LlamaCppServer",
        "ModelProfileOverride": {
          "LoadParams": {
            "MMProjPath": "user://Models/abetlen/BakLLaVA-1-GGUF/mmproj-model-f16.gguf"
          },
          "InferenceParams": {
            "PrePrompt": "",
            "InputPrefix": "",
            "InputSuffix": ""
          }
        }
      }
    }
  }
  
```

The following API request is issued to the `orca-2-13b` model, which is configured to call the `bakllava-1` model in it's Model Definition.

![](https://cdn-uploads.huggingface.co/production/uploads/64b7e345f92b20f7a38bf47a/V5lpOHWGGYJ2yPpEo_8i1.png)

```sh
  $ curl http://localhost:8181/v1/chat/completions --silent \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer no-key" \
    -d '{
      "model": "orca-2-13b",
      "messages": [
        {
          "role": "user",
          "content": [
            {
              "type": "text",
              "text": "Describe this image"
            },
            {
              "type": "image_url",
              "image_url": {
                "url": "https://cdn-uploads.huggingface.co/production/uploads/64b7e345f92b20f7a38bf47a/V5lpOHWGGYJ2yPpEo_8i1.png"
              }
            }
          ]
        }
      ],
      "max_tokens": 300
    }'
```

The output API response's content shows the response to "Describe this image":

> This image shows a tray of 1000 donuts, which are round and fluffy pastries. Some of the donuts have pieces of cheese on top, while others have sauce underneath. These donuts look freshly prepared and delicious, and they might be served at a bakery or a doughnut shop. They could be enjoyed as a sweet treat or a snack with a cup of coffee or tea.  
The output above is `orca-2-13b`'s inference result, using the injected context result from the `bakllava-1` model!

## OpenAI Model Passthrough
By defining a Model Definition and setting the `Backend` property to `openai`, this will trigger OpenAI passthrough and call OpenAI's API with your configured key, returning the result. This allows you to blend your locally running LLMs with OpenAI models such as `gpt-3.5-turbo`, without having to change API endpoint!
```json
  {
    "TextGeneration": {
        "gpt-3.5-turbo": {
        "Backend": "openai"
      },
      "gpt-3.5-turbo-instruct": {
        "Backend": "openai"
      }
    }
  }
```
The configuration for OpenAI API Key is located in the `Config/GodotEGP.Config.GlobalConfig/Config.json` and includes default values where you put your API key from OpenAI.

# Links

- [ElGatoPanzon](https://elgatopanzon.io/)
- [Discord](https://link.elgatopanzon.io/discord)
- [Patreon](https://link.elgatopanzon.io/patreon)
