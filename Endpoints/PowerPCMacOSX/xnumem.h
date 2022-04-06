/*
 *  xnumem.h
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

#pragma once


#include <stddef.h>
#include <stdint.h>

typedef struct kinfo_proc kinfo_proc;
typedef struct vm_region_basic_info vm_region_basic_info;

class xnumem {
public:
	static unsigned char * xnu_read (int pid, uint32_t addr, size_t* size); /* Note : returned buffer must be free'd manually */
	static int xnu_write (int pid, uint32_t addr, unsigned char* data, size_t dsize);
	
	static int32_t procpid (char* procname); /* Process id from name */
	static int getprocessList(kinfo_proc **procList, size_t *procCount);
	
	static int setpage_exec(void *address);
	static size_t _word_align(size_t size);
	
	static uint64_t getAddressOfLibrary( char* libraryPath );
	static uint64_t getAddressOfSymbol(char* libpath, char * symbol);
};