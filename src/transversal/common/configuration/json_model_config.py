from pydantic import ConfigDict

# Los modelos con alias solo se pueden construir por el alias salvo que se diga
# lo contrario: sin esto, AlexaResponseContent(output_speech = ...) falla y hay
# que escribir AlexaResponseContent(outputSpeech = ...) desde codigo Python.
JSON_MODEL_CONFIG: ConfigDict = ConfigDict(populate_by_name = True)