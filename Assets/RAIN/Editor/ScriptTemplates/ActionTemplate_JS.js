import RAIN.Action;
import RAIN.Core;

class ActionTemplate_JS extends RAIN.Action.Action
{
	function newclass()
	{
		actionName = "ActionTemplate_JS";
	}
	
	function Start(agent:Agent, deltaTime:float):ActionResult
	{
        return ActionResult.SUCCESS;
	}

	function Execute(agent:Agent, deltaTime:float):ActionResult
	{
        return ActionResult.SUCCESS;
	}

   	function Stop(agent:Agent, deltaTime:float):ActionResult
	{
        return ActionResult.SUCCESS;
	}
}