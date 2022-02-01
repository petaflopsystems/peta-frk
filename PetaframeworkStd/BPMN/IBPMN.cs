using PetaframeworkStd.Commons;
using System;

namespace PetaframeworkStd.BPMN
{
    public interface IBPMN
    {
        String ToVendorFormat(BusinessProcess bprocess);

        BusinessProcess FromVendorFormat(String xml);

        String FillColor(string hexFillColor, string hexStrokeColor, ProcessTask[] tasksToFill);

        string MergeTasksNames(String xml, BusinessProcess businessProcess);

    }
}
