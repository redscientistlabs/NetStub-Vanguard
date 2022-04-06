/*
 *  xnumem.c
 *
 *  Created by Jonathan Daniel on 05-03-14.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 * The Original Code and all software distributed under the License are
 * distributed on an 'AS IS' basis, WITHOUT WARRANTY OF ANY KIND, EITHER
 * EXPRESS OR IMPLIED, AND APPLE HEREBY DISCLAIMS ALL SUCH WARRANTIES,
 * INCLUDING WITHOUT LIMITATION, ANY WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE, QUIET ENJOYMENT OR NON-INFRINGEMENT.
 * Please see the License for the specific language governing rights and
 * limitations under the License.
 */

#include "xnumem.h"

#include <mach/host_info.h>
#include <mach/mach_host.h>
#include <mach/mach.h>
#include <mach-o/dyld.h>

#include <stdlib.h>
#include <stdio.h>
#import <dlfcn.h>

#include <assert.h>
#include <errno.h>

#include <sys/sysctl.h>
#include <sys/mman.h>

#pragma mark read and write 
/* Note : buffer must be free'd manually */
unsigned char * xnumem::xnu_read (int pid, uint32_t addr, size_t* size)
{	
	assert(*size != 0 || addr != 0);
	
	unsigned char *rbuffer = (unsigned char*)malloc(*size);
	if (rbuffer == 0) 
		printf("Allocation error : xnu_read \n");
	
	mach_msg_type_number_t data_cnt;
	mach_port_t task;
	
	kern_return_t kernret = task_for_pid(mach_task_self(), pid, &task);
	if (kernret != KERN_SUCCESS) 
		printf("Error : task_for_pid \n");
	
	kernret = vm_read(task, (vm_address_t)addr, *size, (vm_offset_t*)&rbuffer, &data_cnt);
	
	if(kernret != KERN_SUCCESS) {
		printf("Error reading memory at 0x%08X. Error code: %d\n", (vm_address_t)addr, kernret);
		free(rbuffer);
	}		
	return rbuffer;
}

int xnumem::xnu_write (int pid, uint32_t addr, unsigned char* data, size_t dsize)
{	
	assert(dsize != 0);
	assert(addr != 0);
	assert(data != 0);
	
	mach_port_t task;
		//vm_info_region_t  regbackup;
	mach_msg_type_number_t dataCunt = dsize;
	
	kern_return_t kret = task_for_pid(mach_task_self(), pid, &task);
	
	if (kret != KERN_SUCCESS)
		printf("xnu_write error: task_for_pid");
	
		//mach_vm_region_info(task, (vm_address_t)addr, &regbackup,0 , 0);
	
	/* retrieve write permision */
	vm_protect(task, (vm_address_t)addr, (vm_size_t)dsize, 0, VM_PROT_READ | VM_PROT_WRITE | VM_PROT_ALL);
	
	kret = vm_write(task, (vm_address_t)addr, (pointer_t)data, dataCunt);
	
	return kret == KERN_SUCCESS ? 0 : 1;
}

mach_error_t xnumem::setpage_exec(void *address) 
{
	mach_error_t err = err_none;
    vm_size_t pageSize;
	
    host_page_size( mach_host_self(), &pageSize );
    uintptr_t page = (uintptr_t)address & ~(uintptr_t)(pageSize-1);
    int e = err_none;
	
    e |= mprotect((void *)page, pageSize, PROT_EXEC | PROT_READ);
    e |= msync((void *)page, pageSize, MS_INVALIDATE );
    if (e) {
        printf("Cannot create executable page\n");
    }
	
    return err;
}


size_t xnumem::_word_align(size_t size)
{
    size_t rsize = 0;
	
    rsize = ((size % sizeof(long)) > 0) ? (sizeof(long) - (size % sizeof(long))) : 0;
    rsize += size;
	
    return rsize;
}

/* Mach-O format related functions */
#pragma mark macho 

/* Retrieve symbol pointer at runtime */
__uint64_t xnumem::getAddressOfSymbol(char* libpath, char * symbol)
{
	void* hlib = dlopen(libpath, RTLD_NOW);
	void* funcaddr64 = dlsym(hlib, symbol); 
	return (unsigned long)funcaddr64;
}


#pragma mark processes 
int32_t xnumem::procpid (char* procname)
{
	pid_t pid;
	int j;
	kinfo_proc * proclist;
	size_t procCount;
	
	getprocessList(&proclist, &procCount);
	
	for (j = 0; j < procCount +1; j++) {
		if (strcmp(proclist[j].kp_proc.p_comm, procname) == 0 ) 
					pid = proclist[j].kp_proc.p_pid;
	}
	
	free(proclist);
	return pid;
}

int xnumem::getprocessList(kinfo_proc **procList, size_t *procCount)
{
    int                 err;
    kinfo_proc *        result;
    int                 done;
    static const int    name[] = { CTL_KERN, KERN_PROC, KERN_PROC_ALL, 0 };

    size_t              length;
	
	assert( procList != NULL);
		//assert(*procList == NULL);
    assert(procCount != NULL);
	
    *procCount = 0;
	
    result = NULL;
    done = 0;
    do {
        assert(result == NULL);
		
        length = 0;
        err = sysctl( (int *) name, (sizeof(name) / sizeof(*name)) - 1,
					 NULL, &length,
					 NULL, 0);
        if (err == -1) {
            err = errno;
        }

		
        if (err == 0) {
            result = (kinfo_proc*)malloc(length);
            if (result == NULL) {
                err = ENOMEM;
            }
        }
		
        if (err == 0) {
            err = sysctl( (int *) name, (sizeof(name) / sizeof(*name)) - 1,
						 result, &length,
						 NULL, 0);
            if (err == -1) {
                err = errno;
            }
            if (err == 0) {
                done = 1;
            } else if (err == ENOMEM) {
                assert(result != NULL);
                free(result);
                result = NULL;
                err = 0;
            }
        }
    } while (err == 0 && ! done);
	
	
    if (err != 0 && result != NULL) {
        free(result);
        result = NULL;
    }
    *procList = result;
    if (err == 0) {
        *procCount = length / sizeof(kinfo_proc);
    }
	
    assert( (err == 0) == (*procList != NULL) );
	
    return err;
}