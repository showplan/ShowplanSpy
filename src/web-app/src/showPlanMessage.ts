interface showplanMessage
{
    showplan: string;
    duration: number;
    estimatedRows: number;
    estimatedCost: number;
    sqlStatement: string;
    queryPlanHashString: string;
    queryPlanHandle: string;
    goodPlan: boolean;
    occuredAt: Date;
}

export { showplanMessage };
