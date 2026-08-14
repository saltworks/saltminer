''' --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
*
* ----
'''

"""
This class conatins all the methods required to change 
the parameters of an ingest pipeline in elastic
"""
import logging
from Core.ElasticClient import ElasticClient



class PipelineParamsUtility:
    """this is the class for the pipeline params utility"""
    def __init__(self, settings, config_file_name) -> None:
        self._es = ElasticClient(settings)
        self.config_file = settings.Get(config_file_name, "parameters")
        self.params_pipeline = 0 

    def pipeline_params_utility(self):
        """This is the runner method to contain all methods for the job"""
        self.get_params_pipeline()
        self.change_params_pipeline()

    def get_params_pipeline(self):
        """This function gets the pipeline to be changed"""
        self.params_pipeline = self._es.GetPipeline(
            pipelineName="params_wrapper_pipeline")
        logging.info("Params pipeline retrieved")

    def change_params_pipeline(self):
        """
        This function changes the pipeline params to reflect what is in the 
        RiskRollerParams.json config file.
        """
        for processor in self.params_pipeline["params_wrapper_pipeline"]["processors"]:
            processor['script']["params"] = self.config_file
        formatted_pipeline = self.format_pipeline()
        self._es.PutPipeline(id="params_wrapper_pipeline", body=formatted_pipeline)
        logging.info("Params pipeline replaced")

    def format_pipeline(self):
        """
        This function changes the pipeline to an Elastic API friendly format
        """
        original_processors = self.params_pipeline['params_wrapper_pipeline']['processors']
        transformed_processors = []
        for processor in original_processors:
            script_processor = processor.get('script', {})
            new_processor = {
                'script': {
                    'source': script_processor['source'],
                    'params': script_processor.get('params', {}),
                    'if': script_processor.get('if', None),
                    'ignore_failure': script_processor.get('ignore_failure', False),
                    'description': script_processor.get('description', '')
                }
            }
            transformed_processors.append(new_processor)
        transformed_pipeline_body = {
            'description': 'Params wrapper pipeline',
            'processors': transformed_processors
        }

        return transformed_pipeline_body
