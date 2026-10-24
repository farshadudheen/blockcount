import { defineConfig } from 'vite';

export default defineConfig({
    build: {
        lib: {
            entry: 'src/index.ts',
            formats: ['es'],
            fileName: 'blockcount',
        },
        outDir: '../BlockCount/wwwroot',
        emptyOutDir: true,
        sourcemap: true,
        rollupOptions: {
            external: [/^@umbraco/],
            output: {
                entryFileNames: 'blockcount.js',
            },
        },
    },
    base: '/App_Plugins/BlockCount/',
    test: {
        environment: 'node',
        include: ['src/**/*.test.ts'],
    },
});
