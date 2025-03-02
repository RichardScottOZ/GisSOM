# -*- coding: utf-8 -*-
"""
Created on Tue Mar 19 11:34:41 2019

@author: shautala

Python script to draw histogram of selected data column for visualizing data distribution in the data preparation stage.
Data is read from numpy arrays saved in binary format.
"""
import warnings
with warnings.catch_warnings():
    warnings.filterwarnings("ignore")
    import matplotlib.pyplot as plt
    import sys
    import numpy as np
    import seaborn as sns

"""
Inputs:
1) Input file
2) Output folder
3) OPTIONAL: noDataValue
"""
inputFile=sys.argv[1]
output_folder=sys.argv[2]
column=np.load(inputFile)  
columnForHistogram=column[6:]   # skip the header of the column, so that only data is used to plotting the histogram

if(len(sys.argv)>3): #if input contains optional nodata argument
    noDataValue=str(float(sys.argv[3]))
    columnForHistogram=columnForHistogram[(columnForHistogram!=noDataValue)]
columnForHistogram=np.genfromtxt(columnForHistogram)
columnForHistogram=columnForHistogram[~np.isnan(columnForHistogram)]
sns.set_style('darkgrid')
plt.style.use('seaborn-darkgrid')
ax=sns.distplot(columnForHistogram.astype(float),kde=False)
plt.title(column[5])
plt.show
plt.savefig(output_folder+'/SomHistogramTest.png')
print(str(column[0]),str(column[1]),str(column[2]),str(column[3]),str(column[4]))